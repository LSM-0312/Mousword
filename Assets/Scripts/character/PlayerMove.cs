using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float moveSpeed = 5f;    
    public float jumpForce = 7f;

    public float fallMultiplier = 2.5f; // 떨어질 때 추가로 받을 중력 배수
    public float lowJumpMultiplier = 2f;    //점프기를 짧게 눌렀을 때 받는 중력

    public bool isGrounded;            // 현재 바닥인지 확인
    public Transform groundCheck;      // 발밑 위치를 정할 오브젝트
    public float checkRadius = 0.2f;   // 감지 범위
    public LayerMask isLayer;          // 바닥으로 인식할 레이어

    // 켜면 좌우 이동과 점프 입력을 무시 (예: 마법사 캐스팅 중). 낙하는 그대로 진행된다.
    public bool IsMovementLocked { get; set; }

    private Rigidbody2D rigid;      // 2D 물리 연산을 다루기 위한 컴포넌트 변수
    private float moveInput;        //키보드 좌/우 입력값을 담아둘 변수

    private Animator anim;                 // 애니메이션 파라미터 제어를 위한 변수
    private SpriteRenderer spriteRenderer; // 좌우 방향 뒤집기용(flipx)


    //씬이 변경되어도 플레이어 게임 오브젝트는 유지
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        //각 컴포넌트들 자동으로 찾아 각 변수에 할당
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();                 
        spriteRenderer = GetComponent<SpriteRenderer>(); 
    }


    void Update()
    {
        // 핵심: 발밑에 가상의 원을 그려서 'Ground' 레이어와 닿아있는지 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, isLayer);

        // 방향키 입력 받음
        moveInput = IsMovementLocked ? 0f : Input.GetAxisRaw("Horizontal");

        // 점프키가 눌리고 바닥에 닿아있는 상태일때 실행
        if (!IsMovementLocked && Input.GetButtonDown("Jump") && isGrounded)
        {
            // 유니티 API인 linearVelocity를 사용하여 X축 속도는 기존 속도를 유지하고, Y축 속도만 jumpForce만큼 뛰아오르게함
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpForce);
        }

        // 떨어질 때 묵직하게(캐릭터가 최고점을 찍고 아래로 떨어지는 중일때 작동)
        if (rigid.linearVelocity.y < 0)
        {
            // 유니티 기본 중력값에 추가 배수를 연산하여 속도에 가산해 아래로 떨어지는 속도를 가속시킴
            rigid.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }

        // 짧은 점프 처리 
        // 캐릭터가 위로 올라가는 중(y > 0) 인데, 점프 버튼(Jump)에서 손을 뗐다면(!Input.GetButton)
        else if (rigid.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            // 중력을 강하게 줘서 상승을 확 꺾어버림
            rigid.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }

        // 캐릭터 좌우 뒤집기
        if (moveInput > 0)
        {
            spriteRenderer.flipX = false; // 오른쪽을 볼 때 (기본)
        }
        else if (moveInput < 0)
        {
            spriteRenderer.flipX = true;  // 왼쪽을 볼 때 (반전)
        }

        // 좌우입력값을 절댓값으로 변환(0 또는 1)하여 애니메이터 Spped 파라미터로 넘겨 이동 및 대기 애니메이션 전환 제어
        anim.SetFloat("Speed", Mathf.Abs(moveInput));

        // 공중 애니메이션 전환을 위한 바닥 상태값을 애니메이터에게 전달
        anim.SetBool("isGrounded", isGrounded);
    }

    // 물리 엔진 주기에 맞춰 실행되는 메서드, 프레임 변동에 영향을 받지 않아 물리 이동 처리에 적합
    void FixedUpdate()
    {
        // X축 속도를 moveInput * moveSpeed로 직접 지정하여 좌우로 정확히 이동하도록 제어
        // Y축 속도는 Update에서 계산된 rigid.linearVelocity.y를 그대로 유지
        rigid.linearVelocity = new Vector2(moveInput * moveSpeed, rigid.linearVelocity.y);
    }
}
