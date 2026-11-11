using UnityEngine;

/// <summary>주문이 커서 위치에서 만들어내는 효과의 종류.</summary>
public enum SpellEffect
{
    None,           // 아직 효과가 구현되지 않은 슬롯 (예: 군중 제어)
    SingleTarget,   // 커서 아래 몬스터 1마리에게만 피해
    Area            // 커서 중심 반경 내 모든 몬스터에게 피해
}

/// <summary>
/// 주문 하나의 정의. 인스펙터에서 직접 편집한다.
/// (스킬이 많아지면 ScriptableObject 로 옮기기 좋은 구조)
/// </summary>
[System.Serializable]
public class SpellSlot
{
    public string name = "주문";
    public SpellEffect effect = SpellEffect.None;

    [Tooltip("피해량.")]
    public float damage = 25f;

    [Tooltip("SingleTarget: 커서 판정 반경(클릭 관용도, 0.3 권장)\n" +
             "Area: 폭발 반경")]
    public float radius = 0.3f;

    public float manaCost = 10f;
    public float cooldown = 0.3f;

    [Tooltip("캐스팅 시간(초). 좌클릭을 이 시간만큼 누르고 있어야 발동한다. 0이면 즉시 발동.")]
    public float castTime = 0f;

    [Tooltip("발현 시 소환할 이펙트 프리팹 (선택). 피해는 SpellCaster 가 주므로 순수 연출용.")]
    public GameObject vfxPrefab;

    [Tooltip("이 주문을 장착했을 때 표시할 조준점 아이콘 번호 (AimReticle 의 배열 인덱스).")]
    public int cursorIndex = 0;

    // 런타임 전용 (저장되지 않음)
    [System.NonSerialized] public float lastUseTime = float.NegativeInfinity;
}

/// <summary>
/// 마법사 시전 관리.
///
/// 핵심 규칙 — 좌클릭은 언제나 "지금 장착된 주문을 커서 위치에 시전"한다.
///   · 평소에는 기본 공격이 장착된 상태
///   · 스킬 키를 누르면 해당 주문이 장착되고 조준점 아이콘이 바뀜
///   · 시전이 끝나면 자동으로 기본 공격으로 복귀
///
/// 캐스팅 규칙:
///   · 좌클릭을 누르고 있는 동안 진행되며, 그동안 캐릭터는 이동·점프할 수 없다
///   · 버튼에서 손을 떼면 취소되고 장착 주문은 유지된다 (마나·쿨다운 소모 없음)
///   · 우클릭 / ESC 는 캐스팅을 취소하고 기본 공격으로 돌아간다
///   · 캐스팅이 끝나는 순간의 커서 위치에 발동하며, 이때 마나·쿨다운이 적용된다
///   · 다시 시전하려면 버튼을 뗐다가 다시 눌러야 한다
///
/// 이 구조 덕에 주문을 늘려도 클릭 처리 코드는 그대로이고 데이터만 추가된다.
///
/// 숫자키 (키보드 상단):
///   1 = 기본 공격, 2 = 범위 공격, 3 = 군중 제어, 4 = 궁극기
///
/// 현재 구현된 효과: SingleTarget(기본 공격), Area(범위 공격·궁극기용)
/// effect 가 None 인 슬롯은 장착은 되지만 시전되지 않음 (조준점 회색)
///
/// 역할 분리:
///   SpellCaster — "언제/시전해도 되는가" (입력·장착·마나·쿨다운·사거리)
///   SpellDamage — "무엇에 얼마나" (피해 판정, 재사용 가능한 순수 로직)
///   SpellVFX    — "어떻게 보이는가" (연출 프리팹, 피해에 관여하지 않음)
///
/// 붙이는 곳: Player_Mage 루트 (PlayerStatus 와 같은 오브젝트).
/// </summary>
public class SpellCaster : MonoBehaviour
{
    [Header("참조 (비우면 자동)")]
    [SerializeField] Camera cam;
    [SerializeField] PlayerStatus status;
    [Tooltip("조준점. 주문 장착 시 커서 아이콘 교체에 사용 (없어도 동작).")]
    [SerializeField] AimReticle reticle;

    [Header("기본 공격 (항상 장착)")]
    [SerializeField] SpellSlot basicAttack = new SpellSlot
    {
        name = "기본 공격",
        effect = SpellEffect.SingleTarget,
        damage = 15f,
        radius = 0.3f,
        manaCost = 0f,
        cooldown = 0.3f,
        castTime = 0.5f,
        cursorIndex = 0
    };

    [Header("스킬 슬롯 (숫자키 2~4)")]
    [Tooltip("0번=키2 범위 공격, 1번=키3 군중 제어, 2번=키4 궁극기. 키1은 기본 공격.\n" +
             "effect 가 None 인 미구현 스킬도 장착은 되지만 시전되지 않는다.")]
    [SerializeField] SpellSlot[] skills = new SpellSlot[3];

    [Header("공통 사거리")]
    [Tooltip("플레이어 기준 최대 시전 거리. 0 이하면 무제한.")]
    [SerializeField] float maxCastRange = 8f;
    [Tooltip("켜면 사거리 밖을 클릭해도 사거리 끝에 시전. 끄면 시전 실패.")]
    [SerializeField] bool clampToRange = true;

    [Header("대상")]
    [Tooltip("피해를 줄 대상 레이어. 기본 Everything.")]
    [SerializeField] LayerMask targetLayer = ~0;

    int equippedIndex = -1;   // -1 = 기본 공격, 0 이상 = skills 배열 번호
    PlayerMove playerMove;    // 캐스팅 중 이동 잠금용 (없어도 동작)

    [System.NonSerialized] SpellSlot castingSpell;   // 캐스팅 중인 주문. null 이면 캐스팅 중이 아님
    float castElapsed;                               // 캐스팅이 진행된 시간(초)

    public bool IsCasting => castingSpell != null;

    /// <summary>지금 좌클릭하면 나갈 주문.</summary>
    public SpellSlot CurrentSpell =>
        (equippedIndex >= 0 && equippedIndex < skills.Length && skills[equippedIndex] != null)
            ? skills[equippedIndex]
            : basicAttack;

    // ── 조준점(AimReticle)·UI 가 참조하는 읽기 전용 상태 ──
    public bool IsOnCooldown => Time.time - CurrentSpell.lastUseTime < CurrentSpell.cooldown;
    public bool HasEnoughMana => status == null || status.CurrentMp >= CurrentSpell.manaCost;
    public bool CanCast =>
        CurrentSpell.effect != SpellEffect.None && !IsCasting && !IsOnCooldown && HasEnoughMana;
    public float MaxCastRange => maxCastRange;
    /// <summary>조준점의 범위 원 표시용 — 현재 주문의 반경.</summary>
    public float CurrentSpellRadius => CurrentSpell.radius;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (status == null) status = GetComponent<PlayerStatus>();
        if (reticle == null) reticle = GetComponentInChildren<AimReticle>();
        playerMove = GetComponent<PlayerMove>();
    }

    void Start()
    {
        EquipBasic();
    }

    // 캐스팅 중 스크립트가 꺼져도 이동 잠금이 남지 않게 한다
    void OnDisable()
    {
        if (IsCasting) StopCasting();
    }

    void Update()
    {
        if (IsCasting)
        {
            UpdateCasting();
            return;
        }

        HandleEquipInput();

        // 우클릭 / ESC 로 조준 취소 → 기본 공격으로 복귀
        if (IsCancelPressed())
            EquipBasic();

        // 누르는 순간에만 시작한다. 누른 채로 유지해도 자동으로 다시 시전되지 않는다.
        if (Input.GetMouseButtonDown(0))
            TryBeginCast();
    }

    bool IsCancelPressed()
    {
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape);
    }

    // ────────────────────────── 주문 장착 ──────────────────────────

    void HandleEquipInput()
    {
        // 키보드 상단 숫자키 (키패드 아님)
        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipBasic();     // 기본 공격
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSkill(0);    // 범위 공격
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSkill(1);    // 군중 제어
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipSkill(2);    // 궁극기
    }

    /// <summary>
    /// 스킬 장착. 미구현(None) 슬롯도 장착되며, 시전은 TryBeginCast 에서 막힌다.
    /// </summary>
    public void EquipSkill(int index)
    {
        if (index < 0 || index >= skills.Length) return;
        if (skills[index] == null) return;

        equippedIndex = index;
        ApplyCursor();
    }

    public void EquipBasic()
    {
        equippedIndex = -1;
        ApplyCursor();
    }

    void ApplyCursor()
    {
        if (reticle != null)
            reticle.SetCursorIndex(CurrentSpell.cursorIndex);
    }

    // ────────────────────────── 시전 ──────────────────────────

    /// <summary>
    /// 마우스 월드 좌표 → 실제로 주문이 발현될 지점.
    /// 조준점과 시전이 같은 함수를 쓰므로 표시와 실제 착탄이 어긋나지 않는다.
    /// </summary>
    public Vector2 ResolveTarget(Vector2 mouseWorld, out bool withinRange)
    {
        withinRange = true;
        if (maxCastRange <= 0f) return mouseWorld;

        Vector2 toTarget = mouseWorld - (Vector2)transform.position;
        if (toTarget.magnitude <= maxCastRange) return mouseWorld;

        withinRange = false;

        return clampToRange
            ? (Vector2)transform.position + toTarget.normalized * maxCastRange
            : mouseWorld;
    }

    /// <summary>캐스팅 시작. 시작 조건만 검사하고 마나·쿨다운은 아직 적용하지 않는다.</summary>
    void TryBeginCast()
    {
        if (cam == null) return;

        SpellSlot spell = CurrentSpell;
        if (spell.effect == SpellEffect.None) return;
        if (IsOnCooldown || !HasEnoughMana) return;

        ResolveTarget(MouseWorldPosition(), out bool withinRange);
        if (!withinRange && !clampToRange) return;

        castingSpell = spell;
        castElapsed = 0f;
        SetMovementLocked(true);

        if (spell.castTime <= 0f)
            CompleteCast();
    }

    /// <summary>캐스팅 중 매 프레임 실행 — 취소 입력을 확인하고 시간을 진행한다.</summary>
    void UpdateCasting()
    {
        // 우클릭 / ESC: 캐스팅과 조준을 모두 취소
        if (IsCancelPressed())
        {
            StopCasting();
            EquipBasic();
            return;
        }

        // 버튼에서 손을 떼면 취소. 장착 주문은 유지되어 바로 다시 시도할 수 있다.
        if (!Input.GetMouseButton(0))
        {
            StopCasting();
            return;
        }

        castElapsed += Time.deltaTime;

        if (castElapsed >= castingSpell.castTime)
            CompleteCast();
    }

    /// <summary>캐스팅 완료 — 이 순간의 커서 위치에 발동하고 마나·쿨다운을 적용한다.</summary>
    void CompleteCast()
    {
        SpellSlot spell = castingSpell;
        StopCasting();

        Vector2 target = ResolveTarget(MouseWorldPosition(), out bool withinRange);
        if (!withinRange && !clampToRange) return;

        // 마나 소모는 가장 마지막 — 앞선 검사에서 실패하면 마나가 낭비되지 않는다.
        if (status != null && !status.ConsumeMp(spell.manaCost)) return;

        spell.lastUseTime = Time.time;
        Fire(spell, target);

        // 스킬은 한 번 쓰면 기본 공격으로 복귀 (기본 공격은 계속 유지)
        if (equippedIndex >= 0) EquipBasic();
    }

    /// <summary>캐스팅 상태를 끝내고 이동 잠금을 푼다.</summary>
    void StopCasting()
    {
        castingSpell = null;
        SetMovementLocked(false);
    }

    void SetMovementLocked(bool locked)
    {
        if (playerMove != null)
            playerMove.IsMovementLocked = locked;
    }

    /// <summary>실제 발현 — 연출과 게임 로직을 각각 담당 클래스에 넘긴다.</summary>
    void Fire(SpellSlot spell, Vector2 target)
    {
        PlayVFX(spell, target);      // 연출 (SpellVFX)
        ApplyEffect(spell, target);  // 게임 로직 (SpellDamage)
    }

    // ────────────────────────── 연출 ──────────────────────────

    /// <summary>이펙트 프리팹 소환. 피해와 무관하며, 없으면 조용히 넘어간다.</summary>
    void PlayVFX(SpellSlot spell, Vector2 target)
    {
        if (spell.vfxPrefab == null) return;

        Instantiate(spell.vfxPrefab, target, Quaternion.identity);
    }

    // ────────────────────────── 게임 로직 ──────────────────────────

    /// <summary>
    /// 효과 종류에 따라 실제 판정을 적용.
    /// 새 효과(텔레포트·군중제어 등)를 추가할 때 SpellEffect 에 값을 넣고
    /// 여기에 case 를 추가하면 된다. 판정 자체는 SpellDamage 등에 위임할 것.
    /// </summary>
    void ApplyEffect(SpellSlot spell, Vector2 target)
    {
        switch (spell.effect)
        {
            case SpellEffect.SingleTarget:
                SpellDamage.ApplySingle(target, spell.radius, spell.damage, targetLayer);
                break;

            case SpellEffect.Area:
                SpellDamage.ApplyArea(target, spell.radius, spell.damage, targetLayer);
                break;
        }
    }

    // ────────────────────────── 유틸 ──────────────────────────

    Vector2 MouseWorldPosition()
    {
        Vector3 ms = Input.mousePosition;
        ms.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        return cam.ScreenToWorldPoint(ms);
    }

    void OnDrawGizmosSelected()
    {
        if (maxCastRange <= 0f) return;
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, maxCastRange);
    }
}
