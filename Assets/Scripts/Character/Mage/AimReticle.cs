using UnityEngine;

/// <summary>
/// 마법사 조준점 — 마우스를 따라다니며 "마법이 실제로 터질 지점"을 표시한다.
///
/// 월드 스프라이트 방식을 쓰는 이유:
///  - 조준점이 월드에 실제로 놓이므로 폭발 반경을 원으로 미리 보여줄 수 있음
///  - 사거리 밖/쿨다운/마나부족 상태를 색으로 즉시 피드백
///  (Cursor.SetCursor 방식은 OS 커서 이미지 교체라 이런 동적 표현이 불가능)
///
/// 위치 계산은 SpellCaster.ResolveTarget 을 그대로 호출한다.
/// → 조준점이 가리키는 곳과 실제 착탄 지점이 절대 어긋나지 않음.
///
/// 붙이는 곳: Player_Mage 의 자식 빈 오브젝트 "AimReticle".
///   (플레이어 자식이면 씬 전환 시에도 함께 유지됨. 씬 루트에 둬도 동작하며,
///    그 경우 caster 를 인스펙터에서 직접 연결할 것)
///
///   AimReticle          ← 이 스크립트
///   ├─ Icon             ← SpriteRenderer (커서 아이콘)
///   └─ RadiusRing       ← SpriteRenderer (속 빈 원, 선택)
/// </summary>
public class AimReticle : MonoBehaviour
{
    [Header("참조 (비우면 자동)")]
    [SerializeField] Camera cam;
    [Tooltip("상태 피드백용. 없으면 위치 추적만 동작.")]
    [SerializeField] SpellCaster caster;
    [Tooltip("커서 아이콘 SpriteRenderer.")]
    [SerializeField] SpriteRenderer icon;
    [Tooltip("폭발 범위를 보여줄 원. 비워두면 범위 표시 없음.")]
    [SerializeField] Transform radiusRing;

    [Header("시스템 커서")]
    [Tooltip("켜면 기본 화살표 커서를 숨김. 이 스크립트가 꺼질 때 자동 복원.")]
    [SerializeField] bool hideSystemCursor = true;

    [Header("스킬별 아이콘")]
    [Tooltip("0번이 기본 커서. 스킬 선택 시 SetCursorIndex(n) 으로 교체.")]
    [SerializeField] Sprite[] cursorSprites;

    [Header("상태 색")]
    [SerializeField] Color normalColor = Color.white;
    [Tooltip("사거리 밖을 조준 중.")]
    [SerializeField] Color outOfRangeColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [Tooltip("쿨다운 중이거나 마나 부족.")]
    [SerializeField] Color cannotCastColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

    [Header("배치")]
    [Tooltip("조준점이 놓일 월드 z. 배경/캐릭터보다 앞이면 됨.")]
    [SerializeField] float zDepth = 0f;
    [Tooltip("범위 원 스프라이트의 원본 지름(월드 유닛). 보통 1.")]
    [SerializeField] float ringUnitDiameter = 1f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (caster == null) caster = GetComponentInParent<SpellCaster>();
        if (icon == null) icon = GetComponentInChildren<SpriteRenderer>();

        if (cursorSprites != null && cursorSprites.Length > 0)
            SetCursorIndex(0);
    }

    void OnEnable()
    {
        if (hideSystemCursor) Cursor.visible = false;
    }

    // 조준점이 꺼지면(메뉴 진입, 사망 등) 시스템 커서를 반드시 되돌려 놓는다.
    void OnDisable()
    {
        if (hideSystemCursor) Cursor.visible = true;
    }

    // 플레이어 이동이 끝난 뒤 조준을 확정 → 캐릭터가 움직여도 조준점이 밀리지 않음
    void LateUpdate()
    {
        if (cam == null) return;

        // 마우스 월드 좌표
        Vector3 ms = Input.mousePosition;
        ms.z = Mathf.Abs(cam.transform.position.z - zDepth);
        Vector2 mouseWorld = cam.ScreenToWorldPoint(ms);

        // 실제 착탄 지점 = 시전에 쓰이는 것과 동일한 계산
        bool withinRange = true;
        Vector2 point = (caster != null)
            ? caster.ResolveTarget(mouseWorld, out withinRange)
            : mouseWorld;

        transform.position = new Vector3(point.x, point.y, zDepth);

        UpdateVisual(withinRange);
    }

    void UpdateVisual(bool withinRange)
    {
        if (caster == null) return;

        // 범위 원을 현재 마법의 폭발 반경에 맞춤 (지름 = 반경 x 2)
        if (radiusRing != null)
        {
            float scale = caster.CurrentSpellRadius * 2f / Mathf.Max(ringUnitDiameter, 0.0001f);
            radiusRing.localScale = new Vector3(scale, scale, 1f);
        }

        if (icon == null) return;

        // 우선순위: 사거리 밖 > 시전 불가(쿨다운·마나) > 정상
        if (!withinRange)      icon.color = outOfRangeColor;
        else if (!caster.CanCast) icon.color = cannotCastColor;
        else                   icon.color = normalColor;
    }

    /// <summary>
    /// 스킬 선택 시 조준점 아이콘 교체. (예: 스킬1 선택 → SetCursorIndex(1))
    /// 나중에 스킬 슬롯 시스템에서 호출하면 됨.
    /// </summary>
    public void SetCursorIndex(int index)
    {
        if (icon == null || cursorSprites == null) return;
        if (index < 0 || index >= cursorSprites.Length) return;

        icon.sprite = cursorSprites[index];
    }
}
