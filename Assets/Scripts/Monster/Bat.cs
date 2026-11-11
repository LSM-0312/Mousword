using UnityEngine;

public class Bat : Monster
{
    protected Rigidbody2D rigid;              // 2D 물리 연산을 다루기 위한 컴포넌트 변수

    public float attackMoveDistance = 2f;                    //박쥐가 플레이어에게 매번 돌진하는 거리
    [HideInInspector] public Vector2 startPosition;                                   //박쥐의 처음 위치
    [HideInInspector] public float curMoveDistance = 0f;                              //박쥐가 이동한 거리
    [HideInInspector] public float moveDirection = 0f;                                //박쥐가 이동할 방향
    [HideInInspector] public Vector2 moveDirVector;                                   //박쥐가 이동할 방향 벡터

    [HideInInspector] public bool isGround;                   //박쥐가 이동할 때 바로앞의 땅을 감지하는 변수
    private float checkRadius = 0.2f;                         //박쥐가 이동할 때 바로앞의 땅을 감지하는 반지름

    protected override void Start()
    {
        base.Start();
        rigid = GetComponent<Rigidbody2D>();

        constMoveSpeed = moveSpeed;                  //박쥐의 이동속도 상수화
        startPosition = transform.position;          //박쥐의 처음 위치 저장

        fsm = new FSM(new BatState.IdleState(this));
    }

    protected override void Update()
    {
        base.Update();

        if (isGround) //코앞에 땅이 있으면
        {
            isWait = true; //대기상태 갱신
        }

        anim.SetBool("Wait", isWait); //대기 애니메이션 상태 전환
    }

    //물리 이동 부분
    //휴식 로직: 가만히 있음
    //공격 로직: 플레이어 위치에 따라 이동 방향 결정 -> 특정 거리만큼 이동 - > 자기 위치 저장 -> 플레이어 바라봄 -> 일정 시간동안 가만히
    void FixedUpdate()
    {
        //가상의 원을 그려서 'Ground' 레이어와 닿아있는지 체크
        //박쥐가 이동할 방향의 바로 앞에 땅이 있는지 확인
        isGround = groundChecker(checkRadius);

        //박쥐가 이동한 거리 계산
        curMoveDistance = Vector2.Distance(startPosition, transform.position);

        //박쥐가 대기 상태일 때
        if (isWait)
        {
            moveSpeed = 0; //이동속도 0으로 설정
        }

        fsm.stateUpdate();

        rigid.linearVelocity = moveDirVector * moveSpeed;  //박쥐 이동
    }

    public void rotate()
    {
        Vector2 direction = (player.position - transform.position).normalized; //플레이어와 박쥐 사이의 방향 벡터 계산, 단위 벡터로 정규화
        moveDirVector = direction;
        moveDirection = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg; //플레이어를 바라보도록 각도 계산
        transform.rotation = Quaternion.Euler(0, 0, moveDirection+180f); //박쥐의 회전 적용

        if (direction.x < 0)
        {
            transform.localScale = new Vector3(1, 1, 1); // 왼쪽일 땐 Y축 반전
        }
        else
        {
            transform.localScale = new Vector3(1, -1, 1);  // 오른쪽일 땐 원상 복구
        }

        //체력바 고정
        GameObject hpBar = transform.Find("HealthBar").gameObject;
        hpBar.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    public override void changeState(state newState)
    {
        switch (newState)
        {
            case state.idle:
                fsm.changeState(new BatState.IdleState(this));
                break;
            case state.attack:
                fsm.changeState(new BatState.AttackState(this));
                break;
            case state.die:
                fsm.changeState(new CommonState.DieState(this));
                break;
            default:
                break;
        }
    }
}
