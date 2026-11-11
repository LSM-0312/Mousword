using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Y축 순서 정렬(LINQ)에 필수

public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private SkillData dashSlashSkill; // 대시 스킬 데이터를 등록
    [SerializeField] private SkillData swordWaveSkill; // 검기 스킬 데이터 등록 
    [SerializeField] private SkillData ultimateSkill;  // 궁극기 스킬 데이터 등록
    [SerializeField] private SkillData spinSkill;

    [SerializeField] private Transform weaponPivot;    // 360도 무기 회전축
    [SerializeField] private SpriteRenderer bodySpriteRenderer; // idle 상태일때 캐릭터가 바라보고있는 방향 참조
    [SerializeField] private LayerMask enemyLayer;      // 궁극기 타격 대상 몬스터 레이어
    [SerializeField] private Collider2D spinHitbox;     // 360도 회전 스킬에서 사용할 검의 충돌 영역

    // 컴포넌트 할당할 변수
    private Rigidbody2D rigid;              
    private PlayerStatus playerStatus;

    private float currentCoolTime = 0f;     // 실시간으로 감소할 현재 남아있는 스킬 쿨타임
    private float swordWaveCoolTime = 0f;     // 검기 스킬 쿨타임
    private float ultimateCoolTime = 0f;   // 궁극기 스킬 쿨타임

    private bool isDashing = false;             // 캐릭터가 현재 대시를 수행 중인지 여부를 저장
    private bool isExecutingUltimate = false;   // 궁극기 시전 중 여부

    private float spinCoolTime = 0f;            // 회전 스킬 쿨타임
    private bool isSpinning = false;            // 회전 상태

    private WeaponAim weaponAim;                // 회전시킬 검 오브젝트의 컴포넌트
    private Rigidbody2D weaponRigid;            // 회전시킬 검 오브젝트의 컴포넌트

    // 현재 검과 충돌 중인 Collider들을 저장
    private readonly List<Collider2D> spinOverlapResults = new List<Collider2D>();
    // 이번 회전 스킬에서 이미 피해를 받은 슬라임 저장
    private readonly HashSet<Slime> spinHitEnemies = new HashSet<Slime>();

    void Start()
    {   
        // 컴포넌트 할당
        rigid = GetComponent<Rigidbody2D>();
        playerStatus = GetComponent<PlayerStatus>();

        if (bodySpriteRenderer == null)
        {
            bodySpriteRenderer = GetComponent<SpriteRenderer>();
        }

        // weaponPivot을 기준으로 WeaponAim 찾기
        if (weaponPivot != null)
        {
            weaponAim = weaponPivot.GetComponent<WeaponAim>();

            // weaponPivot 자체에 없다면 자식에서도 검색
            if (weaponAim == null)
            {
                weaponAim = weaponPivot.GetComponentInChildren<WeaponAim>();
            }

            // WeaponAim이 붙어있는 오브젝트의 Rigidbody2D 가져오기
            if (weaponAim != null)
            {
                weaponRigid = weaponAim.GetComponent<Rigidbody2D>();
            }
        }
    }

    void Update()
    {
        // 쿨타임 타이머 차감(쿨타임이 남아있으면 매 프레임 흐른 시간만큼 차감)
        if (currentCoolTime > 0) currentCoolTime -= Time.deltaTime;     // 대시 스킬 쿨타임 계산
        if (swordWaveCoolTime > 0) swordWaveCoolTime -= Time.deltaTime; // 검기 스킬 쿨타임 계산
        if (ultimateCoolTime > 0) ultimateCoolTime -= Time.deltaTime;   // 궁극기 쿨타임 계산
        if (spinCoolTime > 0) spinCoolTime -= Time.deltaTime;

        // 좌측 Shift 키를 누르고, 쿨타임이 끝났으며, 현재 대시 중이 아닐 때 실행
        if (Input.GetKeyDown(KeyCode.LeftShift) && currentCoolTime <= 0 && !isDashing)
        {
            // 대시 시도
            TryExecuteDash(dashSlashSkill);
        }

        // E 키 또는 마우스 우클릭: 검기 발사 스킬 실행
        if ((Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)) && swordWaveCoolTime <= 0)
        {
            TryExecuteSwordWave(swordWaveSkill);
        }

        // [R 키] 궁극기 범위 순차 타격 스킬
        if (Input.GetKeyDown(KeyCode.R) && ultimateCoolTime <= 0 && !isExecutingUltimate)
        {
            TryExecuteUltimate(ultimateSkill);
        }

        if (Input.GetKeyDown(KeyCode.Q) && spinCoolTime <= 0f && !isSpinning)
        {
            TryExecuteSpin(spinSkill);
        }
    }

    #region 1. 대시 스킬 로직
    private void TryExecuteDash(SkillData skill)
    {
        // 예외 처리(에러 방지)
        if (skill == null) return;

        // PlayerStatus가 제대로 붙어있는지 확인
        if (playerStatus == null)
        {
            Debug.LogError("오류: Player 1 오브젝트에 'PlayerStatus' 스크립트가 없습니다!");
            return;
        }

        // PlayerStatus의 마나를 확인하고 차감 (마나가 충분한지 체크)
        if (playerStatus.CurrentMp >= skill.manaCost)
        {
            playerStatus.ConsumeMp(skill.manaCost); // 마나 소모 함수

            Debug.Log($"[대시 사용] 소모 마나: {skill.manaCost} | 남은 마나: {playerStatus.CurrentMp}/{playerStatus.MaxMp}");

            currentCoolTime = skill.coolTime;       // 쿨타임 적용

            // 대시 물리 연산 코루틴 시작
            StartCoroutine(DashCoroutine(skill));
        }
        else
        {
            Debug.Log("마나가 부족하여 대시를 사용할 수 없습니다!");
        }
    }

    private IEnumerator DashCoroutine(SkillData skill)
    {
        // 대시 상태 활성화
        isDashing = true;

        // 1. 대시 중 플레이어 조작(PlayerMove)이 물리 연산을 방해하지 못하게 비활성화
        PlayerMove playerMove = GetComponent<PlayerMove>();
        if (playerMove != null) playerMove.enabled = false;

        // 2. 대시 중 중력 때문에 밑으로 쳐지는 현상 방지
        float originalGravity = rigid.gravityScale;
        rigid.gravityScale = 0f;

        // 대시 방향 결정
        Vector2 dashDirection = Vector2.right;

        // 키보드 AD나 방향키 입력을 직접 감지 (GetAxisRaw 사용으로 미끄러짐 방지)
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        if (horizontalInput != 0)
        {
            // 키 입력이 있다면 누르고 있는 좌/우 방향으로 대시(-1, 1)
            dashDirection = new Vector2(horizontalInput, 0f).normalized;
        }
        else
        {
            // 2.정지 상태일 때는 직접 연결한 몸통 스프라이트의 flipX 값을 읽어옴
            if (bodySpriteRenderer != null)
            {
                float facingDirection = bodySpriteRenderer.flipX ? -1f : 1f;
                dashDirection = new Vector2(facingDirection, 0f).normalized;
            }
            else
            {
                Debug.LogWarning("방향 추적용 스프라이트가 인스펙터에 연결되지 않았습니다!");
                dashDirection = new Vector2(transform.localScale.x, 0f).normalized;
            }
        }
        // ====================================================================

        // 4. 계산된 대시 방향 벡터에 대시 속도를 곱해 일직선으로 뻗어가도록 이동
        rigid.linearVelocity = dashDirection * skill.dashForce;

        // 5. 대시 이펙트 프리팹 생성 (있을 경우에만)
        if (skill.effectPrefab != null)
        {
            Instantiate(skill.effectPrefab, transform.position, transform.rotation);
        }

        // 6. 스킬 데이터에 정의된 대시 지속 시간만큼 대기
        yield return new WaitForSeconds(skill.dashDuration);

        // 7. 대시 종료 후 정지 및 중력/스크립트 상태 원상복구
        rigid.linearVelocity = Vector2.zero;    // 대기 지속 시간이 지나면 밀리는 관성을 지우기 휘애 속도를 즉시 0으로 만듦
        rigid.gravityScale = originalGravity;   // 대시 시작 전에 백업해 두었던 원래 중력 값으로 원복

        if (playerMove != null) playerMove.enabled = true; // 이동 제어 스크립트 복구

        isDashing = false;
    }
    #endregion

    #region 2. 검기 스킬 로직
    private void TryExecuteSwordWave(SkillData skill)
    {
        if (skill == null || skill.projectilePrefab == null) return;

        if (playerStatus == null)
        {
            Debug.LogError("오류: Player 1 오브젝트에 'PlayerStatus' 스크립트가 없습니다!");
            return;
        }

        // 마나 검증 및 차감
        if (playerStatus.CurrentMp >= skill.manaCost)
        {
            playerStatus.ConsumeMp(skill.manaCost);
            Debug.Log($"[검기 발사] 소모 마나: {skill.manaCost} | 남은 마나: {playerStatus.CurrentMp}/{playerStatus.MaxMp}");

            swordWaveCoolTime = skill.coolTime; // 쿨타임 적용
            ShootSwordWave(skill);             // 실제 투사체 생성
        }
        else
        {
            Debug.Log("마나가 부족하여 검기를 발사할 수 없습니다!");
        }
    }

    private void ShootSwordWave(SkillData skill)
    {
        if (weaponPivot == null)
        {
            Debug.LogWarning("Weapon Pivot이 인스펙터에 연결되지 않았습니다!");
            return;
        }

        // 1. 마우스를 바라보고 있는 weaponPivot의 위치와 회전값(rotation)으로 검기 생성
        GameObject waveObj = Instantiate(skill.projectilePrefab, weaponPivot.position, weaponPivot.rotation);

        // 2. 검기 스크립트에 속도 및 데미지 전달
        SwordWave swordWave = waveObj.GetComponent<SwordWave>();
        if (swordWave != null)
        {
            swordWave.Initialize(skill.projectileSpeed, skill.damage);
        }
    }
    #endregion

    #region 3. 궁극기(화면 전체 순차 타격) 로직
    private void TryExecuteUltimate(SkillData skill)
    {
        if (skill == null) return;

        if (playerStatus == null)
        {
            Debug.LogError("오류: Player 1 오브젝트에 'PlayerStatus' 스크립트가 없습니다!");
            return;
        }

        if (playerStatus.CurrentMp >= skill.manaCost)
        {
            playerStatus.ConsumeMp(skill.manaCost);
            Debug.Log($"[궁극기 발동] 소모 마나: {skill.manaCost} | 남은 마나: {playerStatus.CurrentMp}/{playerStatus.MaxMp}");

            ultimateCoolTime = skill.coolTime;

            // 순수 순차 타격 기능 코루틴 시작
            StartCoroutine(ExecuteUltimateHitSequence());
        }
        else
        {
            Debug.Log("마나가 부족하여 궁극기를 사용할 수 없습니다!");
        }
    }

    // 지금은 R키로 바로 작동하며, 나중에 모션 완성 시 애니메이션 이벤트로 호출할 핵심 기능
    public IEnumerator ExecuteUltimateHitSequence()
    {
        if (ultimateSkill == null) yield break;

        isExecutingUltimate = true;

        // 1. 현재 화면(메인 카메라) 영역 좌표 및 크기 계산
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("Main Camera를 찾을 수 없습니다!");
            isExecutingUltimate = false;
            yield break;
        }

        Vector2 center = mainCam.transform.position;
        Vector2 size = new Vector2(mainCam.orthographicSize * 2 * mainCam.aspect, mainCam.orthographicSize * 2);

        // 2. 화면 전체 영역 안의 모든 enemyLayer 몬스터 수집
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);

        // 3. Y축 기준 내림차순 정렬 (위쪽 Y좌표 -> 아래쪽 Y좌표 순서)
        List<Collider2D> sortedEnemies = hitEnemies
            .OrderByDescending(e => e.transform.position.y)
            .ToList();

        // 4. 위쪽 몬스터부터 순차적으로 데미지 및 이펙트 처리
        foreach (Collider2D enemy in sortedEnemies)
        {
            if (enemy == null) continue;

            // 몬스터 데미지 전달 처리 (프로젝트의 몬스터 스크립트에 맞춰 호출)
            Slime monster = enemy.GetComponentInParent<Slime>();
            if (monster != null)
            {
                monster.TakeDamage((int)ultimateSkill.damage);
            }

            Debug.Log($"[궁극기 순차 피격] {enemy.name} 피격! 데미지: {ultimateSkill.damage}");

            // 피격 지점 이펙트 생성
            if (ultimateSkill.ultimateHitEffectPrefab != null)
            {
                Instantiate(ultimateSkill.ultimateHitEffectPrefab, enemy.transform.position, Quaternion.identity);
            }

            // 위에서 아래로 순차적 피격 느낌을 주는 지연 시간 (SkillData.hitInterval)
            yield return new WaitForSeconds(ultimateSkill.hitInterval);
        }

        isExecutingUltimate = false;
    }

    private void OnDrawGizmosSelected()
    {
        // 카메라 감지 영역 Gizmo 표시
        if (Camera.main != null)
        {
            Gizmos.color = Color.red;
            Camera mainCam = Camera.main;
            Vector3 center = mainCam.transform.position;
            Vector3 size = new Vector3(mainCam.orthographicSize * 2 * mainCam.aspect, mainCam.orthographicSize * 2, 1);
            Gizmos.DrawWireCube(center, size);
        }
    }
    #endregion

    #region 4. 360도 회전 스킬

    private void TryExecuteSpin(SkillData skill)
    {
        // 스킬 데이터가 없으면 실행 안 함
        if (skill == null)
        {
            Debug.LogWarning("Spin Skill 데이터가 없습니다.");
            return;
        }

        // PlayerStatus 확인
        if (playerStatus == null)
        {
            Debug.LogWarning("PlayerStatus가 없습니다.");
            return;
        }

        // 검 확인
        if (weaponAim == null || weaponRigid == null)
        {
            Debug.LogWarning("WeaponAim 또는 무기의 Rigidbody2D를 찾지 못했습니다.");
            return;
        }

        if (spinHitbox == null)
        {
            Debug.LogWarning(
                "Spin Hitbox가 연결되지 않았습니다."
            );

            return;
        }

        // 마나가 부족하면 실행 안 함
        if (playerStatus.CurrentMp < skill.manaCost)
        {
            Debug.Log("마나가 부족해서 회전 스킬을 사용할 수 없습니다.");
            return;
        }

        // 마나 소모
        playerStatus.ConsumeMp(skill.manaCost);

        // 쿨타임 적용
        spinCoolTime = skill.coolTime;

        // 실제 검 회전 시작
        StartCoroutine(SpinSwordCoroutine(skill));
    }


    private IEnumerator SpinSwordCoroutine(SkillData skill)
    {
        isSpinning = true;

        // 이전 회전 스킬의 피격 기록 제거
        spinHitEnemies.Clear();

        // 기존 평타의 누적 회전각 초기화
        if (weaponAim != null)
        {
            weaponAim.ConsumeSwing();

            // 마우스 방향 추적 중지
            weaponAim.enabled = false;
        }


        // =========================================
        // 2. 회전 정보 계산
        // =========================================

        float rotatedAngle = 0f;

        float totalAngle = Mathf.Abs(skill.spinDegrees);

        float duration = Mathf.Max(
            skill.spinDuration,
            0.01f
        );

        // 예:
        // 360도 / 0.5초 = 초당 720도 회전
        float spinSpeed =
            totalAngle / duration;


        // =========================================
        // 3. 검을 실제로 360도 회전
        // =========================================

        while (rotatedAngle < totalAngle)
        {
            float rotateAmount =
                spinSpeed * Time.fixedDeltaTime;


            // 마지막 프레임에 360도를 넘어가는 현상 방지
            rotateAmount = Mathf.Min(
                rotateAmount,
                totalAngle - rotatedAngle
            );


            // 현재 검의 각도에서 조금씩 회전
            float nextAngle =
                weaponRigid.rotation + rotateAmount;


            weaponRigid.MoveRotation(nextAngle);


            // 지금까지 얼마나 돌았는지 기록
            rotatedAngle += rotateAmount;


            // 다음 물리 프레임까지 기다림
            yield return new WaitForFixedUpdate();

            // 검에 닿은 슬라임 공격
            CheckSpinDamage(skill);
        }


        // 평타의 회전각 기록 초기화 후 조준 복구
        if (weaponAim != null)
        {
            weaponAim.ConsumeSwing();
            weaponAim.enabled = true;
        }

        // 이번 스킬의 피격 기록 초기화
        spinHitEnemies.Clear();

        isSpinning = false;
    }

    // 회전 스킬의 몬스터 공격 판정
    private void CheckSpinDamage(SkillData skill)
    {
        // 검의 충돌 영역이 없다면 실행하지 않음
        if (spinHitbox == null)
            return;

        // 이전 검사 결과 초기화
        spinOverlapResults.Clear();

        // 몬스터 레이어만 검사
        ContactFilter2D filter = new ContactFilter2D();

        filter.SetLayerMask(enemyLayer);
        filter.useTriggers = true;

        // 현재 검과 실제로 겹쳐 있는 Collider 검색
        Physics2D.OverlapCollider(
            spinHitbox,
            filter,
            spinOverlapResults
        );

        // 검에 닿은 모든 Collider 확인
        foreach (Collider2D hit in spinOverlapResults)
        {
            if (hit == null)
                continue;

            // Collider의 부모에서 슬라임 검색
            Slime monster =
                hit.GetComponentInParent<Slime>();

            if (monster == null)
                continue;

            // 이미 피해를 받은 슬라임이면 무시
            if (!spinHitEnemies.Add(monster))
                continue;

            // 처음 맞은 슬라임에게만 데미지 적용
            monster.TakeDamage((int)skill.damage);

            Debug.Log(
                $"[회전 스킬 적중] {monster.name} " +
                $"데미지: {skill.damage}"
            );
        }
    }

    #endregion
}



