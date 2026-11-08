using UnityEngine;

/// <summary>
/// 항아리 게임(Getting Over It) 방식 등반 조작 — 기하(지오메트리) 기반 재설계.
///
/// 설계 철학: 조인트/토크/모터를 전부 사용하지 않는다.
///  - 검은 별도 Rigidbody2D 가 아니라, 플레이어 Rigidbody2D 에 붙은 "자식 콜라이더".
///  - 검의 회전/뻗기는 힘이 아니라 transform 을 직접 움직임 (운동학적 제어).
///  - 등반은 물리 엔진의 충돌 해소가 공짜로 해줌:
///      검 끝이 땅에 닿은 채로 검을 돌리면, "몸의 일부가 땅을 뚫을 수 없다"는
///      제약을 지키기 위해 엔진이 플레이어 전체를 밀어냄 = 등반.
///
/// 이 구조의 장점:
///  - 공중 반동 0 (힘을 안 쓰니 반작용 자체가 없음)
///  - 마우스 추적 즉각적 (PD/모터 튜닝, 방향 부호 문제 없음)
///  - braced 판정, groundLayer, 등반 토크 튜닝 전부 불필요
///  - 마우스 거리에 따른 뻗기/당기기(원작의 포고 동작)가 localPosition 한 줄로 가능
///
/// 하이어라키 (필수 구조):
///   Player_Climb  ← Rigidbody2D(Dynamic, Freeze Z), 몸통 콜라이더, 이 스크립트
///   └─ SwordPivot ← 빈 오브젝트, 손 위치. 이 transform 이 마우스 방향으로 회전
///      └─ Sword   ← SpriteRenderer + 콜라이더(Is Trigger 끔). Rigidbody2D 없음!
///
/// 검 스프라이트 방향: 피벗의 +X(오른쪽)가 마우스를 향합니다.
///   검 스프라이트가 위를 향해 그려져 있으면 Sword "자식"의 로컬 Z 회전을 -90 으로
///   맞춰서 칼날이 부모의 +X 를 향하게 하세요. (스크립트가 아니라 에디터에서 1회 설정)
///
/// 주의:
///  - 검/피벗에 Rigidbody2D 나 HingeJoint2D 를 붙이면 안 됩니다 (구조가 무너짐).
///  - 이 프리팹에는 PlayerMove 를 넣지 마세요 (수평 속도를 덮어써서 등반을 막음).
///  - 기존 SwordPhysicsControl 방식과는 완전히 별개의 대안 구현입니다.
/// </summary>
public class SwordClimbControl : MonoBehaviour
{
    [Header("참조 (비우면 자동)")]
    [SerializeField] Camera cam;
    [Tooltip("검이 회전할 축(손 위치). 플레이어의 자식 빈 오브젝트.")]
    [SerializeField] Transform swordPivot;
    [Tooltip("피벗의 자식인 검 본체. 뻗기(reach) 기능에만 사용, 비우면 뻗기 없이 회전만.")]
    [SerializeField] Transform sword;

    [Header("회전 (마우스 방향 조준)")]
    [Tooltip("초당 최대 회전 속도(도). 너무 크면 검이 지면을 깊이 파고들어 튕기고, 너무 작으면 굼뜹니다. 360~720 권장.")]
    [SerializeField] float rotationSpeed = 540f;

    [Header("뻗기 (마우스 거리 추적)")]
    [Tooltip("끄면 검 길이 고정(회전만). 켜면 마우스가 멀수록 검을 뻗음 — 원작의 당겼다 뻗기.")]
    [SerializeField] bool enableReach = true;
    [Tooltip("검을 최대한 당겼을 때 피벗~검 손잡이 거리.")]
    [SerializeField] float minReach = 0.2f;
    [Tooltip("검을 최대한 뻗었을 때 피벗~검 손잡이 거리.")]
    [SerializeField] float maxReach = 1.0f;
    [Tooltip("초당 최대 뻗기/당기기 속도(유닛). 포고(찍고 튀기)의 힘은 이 값에서 나옵니다.")]
    [SerializeField] float reachSpeed = 6f;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    // 물리 힘을 다루지 않으므로 Update 에서 처리 (화면 주사율만큼 부드럽게).
    // 물리 엔진은 다음 고정 스텝에서 바뀐 콜라이더 위치를 반영해 충돌을 해소한다.
    void Update()
    {
        if (cam == null || swordPivot == null) return;

        // 마우스 월드 좌표
        Vector3 ms = Input.mousePosition;
        ms.z = Mathf.Abs(cam.transform.position.z);
        Vector2 mouseWorld = cam.ScreenToWorldPoint(ms);

        Vector2 dir = mouseWorld - (Vector2)swordPivot.position;
        if (dir.sqrMagnitude < 1e-4f) return;   // 마우스가 손 위치에 겹치면 무시

        // ── 회전: 피벗의 +X 가 마우스를 향하도록, 속도 제한을 걸어 따라감 ──
        // MoveTowardsAngle 이 360도 경계(-180/+180)를 알아서 처리해 최단 방향으로 회전.
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float nextAngle = Mathf.MoveTowardsAngle(
            swordPivot.eulerAngles.z, targetAngle, rotationSpeed * Time.deltaTime);
        swordPivot.rotation = Quaternion.Euler(0f, 0f, nextAngle);

        // ── 뻗기: 마우스가 멀면 검을 뻗고 가까우면 당김 (minReach ~ maxReach) ──
        if (enableReach && sword != null)
        {
            float targetReach = Mathf.Clamp(dir.magnitude, minReach, maxReach);
            float nextReach = Mathf.MoveTowards(
                sword.localPosition.x, targetReach, reachSpeed * Time.deltaTime);
            sword.localPosition = new Vector3(nextReach, sword.localPosition.y, sword.localPosition.z);
        }
    }
}
