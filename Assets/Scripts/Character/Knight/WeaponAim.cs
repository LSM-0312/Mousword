using UnityEngine;

public class WeaponAim : MonoBehaviour
{
    Camera mainCam;
    Rigidbody2D rb;

    public float maxAngularSpeed = 500f;   // 최대 회전 속도
    public float angularAccel = 1000f;     // 회전 가속도
    public float angularDrag = 3000f;      // 목표 근처에서 감속하는 힘
    public float stopAngle = 10f;          // 이 각도 이내면 정지 처리

    public float requiredSwingAngle = 90f; // 공격 가능에 필요한 한 방향 누적 각도

    float targetAngle;
    float currentAngularSpeed;

    float lastAngle;
    float accumulatedSwingAngle;
    float swingDirection;

    public bool CanAttack
    {
        get { return accumulatedSwingAngle >= requiredSwingAngle; }
    }

    void Start()
    {
        mainCam = Camera.main;
        rb = GetComponent<Rigidbody2D>();

        lastAngle = rb.rotation;
    }

    void Update() // 매 프레임마다 마우스 위치를 읽어 목표 각도를 갱신
    {
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition); // 마우스 위치를 월드 좌표로 변환
        mousePos.z = 0f;

        Vector2 dir = mousePos - transform.position;

        targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void FixedUpdate() // 물리 스텝마다 회전 속도를 계산하고 적용
    {
        float currentAngle = rb.rotation;

        float delta = Mathf.DeltaAngle(currentAngle, targetAngle);

        if (Mathf.Abs(delta) <= stopAngle) // 현재 각도가 목표 각도에 충분히 가까우면 감속
        {
            currentAngularSpeed = Mathf.MoveTowards(
                currentAngularSpeed,
                0f,
                angularDrag * Time.fixedDeltaTime
            );
        }
        else // 목표 각도와 충분히 떨어져 있으면 가속
        {
            float desiredDirection = Mathf.Sign(delta);

            currentAngularSpeed += desiredDirection * angularAccel * Time.fixedDeltaTime;

            currentAngularSpeed = Mathf.Clamp(
                currentAngularSpeed,
                -maxAngularSpeed,
                maxAngularSpeed
            );
        }

        float nextAngle = currentAngle + currentAngularSpeed * Time.fixedDeltaTime;

        rb.MoveRotation(nextAngle);

        AddSwingAngle(currentAngle, nextAngle);
    }

    void AddSwingAngle(float fromAngle, float toAngle) // 회전한 각도 누적 함수
    {
        float movedDelta = Mathf.DeltaAngle(fromAngle, toAngle); // DeltaAngle 은 -180~180 범위로 반환되므로, 이동한 각도는 절대값으로 누적

        if (Mathf.Abs(movedDelta) < 1f)
            return;

        float movedDirection = Mathf.Sign(movedDelta);

        if (swingDirection == 0f)
        {
            swingDirection = movedDirection;
            accumulatedSwingAngle = Mathf.Abs(movedDelta);
            return;
        }

        if (movedDirection == swingDirection)
        {
            accumulatedSwingAngle += Mathf.Abs(movedDelta);
        }
        else
        {
            swingDirection = movedDirection;
            accumulatedSwingAngle = Mathf.Abs(movedDelta);
        }

        lastAngle = toAngle;
    }

    public void ConsumeSwing() // 공격이 성공적으로 적중했을 때 누적 각도를 초기화
    {
        accumulatedSwingAngle = 0f;
        swingDirection = 0f;
    }
}