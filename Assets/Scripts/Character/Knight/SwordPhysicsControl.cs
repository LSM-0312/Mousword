using UnityEngine;

/// <summary>
/// Getting Over It - 물리 버전 (공중 스윙 / 등반 토크 분리).
///
/// 구조:
///  - 검(Sword)은 Dynamic Rigidbody2D + 콜라이더 → 정적 지면에 실제로 막힘.
///  - Player 와 HingeJoint2D 로 손에서 연결.
///  - 토크를 두 종류로 나눠서 줌:
///      (A) 조준 토크: 항상. 검을 마우스로 돌림. 검 관성(질량)에 비례 → 가볍게 두면 공중 반력 작음.
///      (B) 등반 토크: 검 콜라이더가 지면에 "닿아 있을 때만". 질량과 무관하게 강함.
///                    막힌 검을 미는 힘 → 힌지 반력으로 캐릭터가 등반.
///
/// 즉 공중에선 (A)만 작동(거의 안 흔들림), 지면에 박혔을 땐 (B)가 더해져 강하게 등반.
///
/// 붙이는 곳: "Sword" (Dynamic Rigidbody2D + HingeJoint2D + 콜라이더, Gravity 0 권장).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SwordPhysicsControl : MonoBehaviour
{
    [Header("참조 (비우면 자동)")]
    [SerializeField] Camera cam;
    [SerializeField] Rigidbody2D swordRb;
    [SerializeField] HingeJoint2D hinge;

    [Header("스프라이트 보정")]
    [Tooltip("검 날이 로컬 +X 가 아니면 보정(도). 위를 향하면 -90.")]
    [SerializeField] float bladeAngleOffset = 0f;

    [Header("조준 토크 (항상, 검을 돌림)")]
    [SerializeField] float angleP = 20f;
    [SerializeField] float angularD = 6f;
    [Tooltip("공중에서 검을 돌릴 때 토크 상한. 작게 둘수록 공중 반력↓. (등반력과 별개)")]
    [SerializeField] float maxAimTorque = 40f;
    [SerializeField] float maxAngularSpeed = 720f;
    [Tooltip("목표각에 이 각도 이내면 미세 토크 정지(떨림 방지).")]
    [SerializeField] float angleDeadZone = 1.5f;
    [SerializeField] float settleDamp = 600f;

    [Header("등반 토크 (지면에 닿았을 때만)")]
    [Tooltip("지면으로 인정할 레이어 (검 콜라이더 충돌 판정용).")]
    [SerializeField] LayerMask groundLayer;
    [Tooltip("등반 토크 세기(질량 무관). 등반 약하면 이 값을 키우세요.")]
    [SerializeField] float climbGain = 40f;
    [Tooltip("등반 토크 상한(=캐릭터를 미는 힘의 최대).")]
    [SerializeField] float maxClimbTorque = 400f;

    // [추가] 코요테 타임: 검이 지면에서 떨어져도 이 시간(초) 동안은
    //        계속 "닿아 있는 것"으로 취급 → 미세한 튕김에 등반 토크가 끊기는 것 방지.
    [Tooltip("지면 접촉 유예 시간(초). 검이 살짝 튕겨도 등반 토크가 유지됨.")]
    [SerializeField] float coyoteTime = 0.1f;

    // bool touchingGround;     // 직전 물리 스텝에서 검이 지면에 닿았는지  ← [변경] 아래 시각 기록으로 대체
    // [추가] bool 순간값 대신 "마지막으로 지면에 닿은 시각"을 기록 (코요테 타임의 기준점)
    float lastGroundContactTime = float.NegativeInfinity;

    Vector2 Pivot => hinge != null ? (Vector2)transform.TransformPoint(hinge.anchor) : swordRb.position;

    void Reset()
    {
        swordRb = GetComponent<Rigidbody2D>();
        hinge = GetComponent<HingeJoint2D>();
    }

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (swordRb == null) swordRb = GetComponent<Rigidbody2D>();
        if (hinge == null) hinge = GetComponent<HingeJoint2D>();
    }

    void FixedUpdate()
    {
        // bool braced = touchingGround;
        // touchingGround = false;   // 이번 스텝 충돌은 OnCollisionStay2D 가 다시 채움
        // [변경] 마지막 접촉 후 coyoteTime 이내면 계속 braced 로 취급 (유예 판정)
        bool braced = Time.time - lastGroundContactTime <= coyoteTime;

        if (cam == null || swordRb == null) return;

        Vector3 ms = Input.mousePosition;
        ms.z = Mathf.Abs(cam.transform.position.z);
        Vector2 mouseWorld = cam.ScreenToWorldPoint(ms);

        Vector2 dir = mouseWorld - Pivot;
        if (dir.sqrMagnitude < 1e-4f) return;

        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + bladeAngleOffset;
        float angleError = Mathf.DeltaAngle(swordRb.rotation, targetAngle);

        // (A) 조준 토크 — 검을 마우스로 돌림. 검 관성에 비례(가벼우면 공중 반력 작음).
        if (Mathf.Abs(angleError) < angleDeadZone)
        {
            swordRb.angularVelocity =
                Mathf.MoveTowards(swordRb.angularVelocity, 0f, settleDamp * Time.fixedDeltaTime);
        }
        else
        {
            float angAccel = angleError * angleP - swordRb.angularVelocity * angularD;
            float aimTorque = Mathf.Clamp(angAccel * Mathf.Max(swordRb.inertia, 0.0001f),
                                          -maxAimTorque, maxAimTorque);
            swordRb.AddTorque(aimTorque);
        }

        // (B) 등반 토크 — 검이 지면에 닿아 있을 때만. 질량 무관, 강함.
        if (braced)
        {
            float climbTorque = Mathf.Clamp(angleError * climbGain, -maxClimbTorque, maxClimbTorque);
            swordRb.AddTorque(climbTorque);
        }

        if (Mathf.Abs(swordRb.angularVelocity) > maxAngularSpeed)
            swordRb.angularVelocity = Mathf.Sign(swordRb.angularVelocity) * maxAngularSpeed;
    }

    void OnCollisionStay2D(Collision2D c)
    {
        if (((1 << c.gameObject.layer) & groundLayer) != 0)
            // touchingGround = true;
            // [변경] 접촉한 "시각"을 기록 → FixedUpdate 에서 코요테 타임 판정에 사용
            lastGroundContactTime = Time.time;
    }
}
