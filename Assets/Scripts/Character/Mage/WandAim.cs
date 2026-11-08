using UnityEngine;

/// <summary>
/// 마법사 지팡이 조준 — 마우스 방향을 따라 회전하기만 하는 시각 전용 스크립트.
///
/// 물리 타격이 없으므로 Rigidbody2D / 콜라이더가 필요 없고,
/// 그래서 transform 을 직접 회전시킨다 (FixedUpdate 50Hz 제약 없이 화면 주사율 그대로).
///
/// 붙이는 곳: WandPivot (플레이어의 자식 빈 오브젝트, 손 위치).
///   Player_Mage
///   └─ WandPivot   ← 이 스크립트
///      └─ Wand     ← SpriteRenderer 만. Rigidbody2D·콜라이더 없음
///         └─ CastPoint  ← 지팡이 끝 (VFX 시작점, 나중에 사용)
///
/// 규약: 피벗의 로컬 +X(오른쪽)가 마우스를 향한다.
///   지팡이 스프라이트가 위를 향해 그려져 있으면 spriteAngleOffset 을 -90 으로.
/// </summary>
public class WandAim : MonoBehaviour
{
    [Header("참조 (비우면 자동)")]
    [SerializeField] Camera cam;

    [Header("조준")]
    [Tooltip("0 = 즉시 조준(기본). 값을 주면 그 속도(도/초)로 부드럽게 따라옴 — 연출용.\n" +
             "마법은 커서 위치에 발현되므로, 지팡이가 늦게 따라와도 게임플레이엔 영향 없음.")]
    [SerializeField] float followSpeed = 0f;

    [Header("스프라이트 보정")]
    [Tooltip("지팡이 끝이 로컬 +X 가 아니면 보정(도). 위를 향하면 -90.")]
    [SerializeField] float spriteAngleOffset = 0f;
    [Tooltip("왼쪽을 조준할 때 지팡이가 위아래로 뒤집혀 보이는 것을 방지 (localScale.y 반전).")]
    [SerializeField] bool autoFlip = true;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    // LateUpdate 사용 이유:
    //  - 플레이어 이동/애니메이션이 모두 끝난 뒤에 조준을 확정 → 1프레임 밀림 없음
    //  - Animator 가 자식 transform 을 건드려도 그 위에 덮어쓸 수 있음
    void LateUpdate()
    {
        if (cam == null) return;

        // 마우스 월드 좌표 (원근 카메라도 대응되도록 카메라~피벗 거리로 z 지정)
        Vector3 ms = Input.mousePosition;
        ms.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector2 mouseWorld = cam.ScreenToWorldPoint(ms);

        Vector2 dir = mouseWorld - (Vector2)transform.position;
        if (dir.sqrMagnitude < 1e-4f) return;   // 마우스가 손 위치에 겹치면 무시

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffset;

        // followSpeed 가 0 이면 즉시, 아니면 최단 경로로 속도 제한을 걸어 따라감.
        float nextAngle = (followSpeed <= 0f)
            ? targetAngle
            : Mathf.MoveTowardsAngle(transform.eulerAngles.z, targetAngle,
                                     followSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, 0f, nextAngle);

        // 왼쪽 조준 시 스프라이트가 뒤집혀 보이는 문제 보정.
        // 로컬 Y 를 반전 = 지팡이 길이축(X) 기준 거울반전이라 조준 방향은 그대로 유지됨.
        if (autoFlip)
        {
            Vector3 s = transform.localScale;
            s.y = (dir.x < 0f) ? -Mathf.Abs(s.y) : Mathf.Abs(s.y);
            transform.localScale = s;
        }
    }
}
