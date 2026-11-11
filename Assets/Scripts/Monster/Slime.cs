using UnityEngine;

public class Slime : Monster
{
    protected Rigidbody2D rigid;              // 2D 물리 연산을 다루기 위한 컴포넌트 변수

    public float maxIdleMoveDistance = 3f;         //슬라임이 플레이어를 인식하지 않을 때 이동할 수 있는 최대 거리
    public float noticeMoveDistance = 1f;          //슬라임이 플레이어를 인식했을때 매번 이동하는 거리
    [HideInInspector] public Vector2 startPosition;                                   //슬라임의 처음 위치
    [HideInInspector] public float curMoveDistance = 0f;                              //슬라임이 이동한 거리
    [HideInInspector] public int moveDirection = 1;                                   //슬라임이 이동할 방향(1: 오른쪽, -1: 왼쪽)

    [HideInInspector] public bool isGround;                   //슬라임이 이동할 때 바로앞의 땅을 감지하는 변수
    private float checkRadius = 0.2f;       //슬라임이 이동할 때 바로앞의 땅을 감지하는 반지름

    protected override void Start()
    {
        base.Start();
        rigid = GetComponent<Rigidbody2D>();

        constMoveSpeed = moveSpeed;                  //슬라임의 이동속도 상수화
        startPosition = transform.position;          //슬라임의 처음 위치 저장

        fsm = new FSM(new SlimeState.IdleState(this));
    }

    protected override void Update()
    {
        base.Update();
        if (!isGround) //앞에 땅이 없으면
        {
            isWait = true; //대기상태 갱신
        }

        anim.SetBool("Wait", isWait); //대기 애니메이션 상태 전환
        fsm.stateUpdate();
    }

    //물리 이동 부분
    //이동 로직: 현재 자기 위치 저장 -> 플레이어 인식 여부에 따라 이동 방향 결정 -> 특정 거리만큼 이동 -> 자기 위치 저장
    void FixedUpdate()
    {
        //가상의 원을 그려서 'Ground' 레이어와 닿아있는지 체크
        //슬라임이 이동할 방향의 바로 앞에 땅이 있는지 확인
        isGround = groundChecker(checkRadius);

        //슬라임이 이동한 거리 계산
        curMoveDistance = Mathf.Abs(transform.position.x - startPosition.x);

        //슬라임이 대기 상태일 때
        if (isWait)
        {
            moveSpeed = 0; //이동속도 0으로 설정
        }

        

        rigid.linearVelocity = new Vector2(moveDirection * moveSpeed, rigid.linearVelocity.y);  //슬라임 이동
    }

    public override void changeState(state newState)
    {
        switch (newState)
        {
            case state.idle:
                fsm.changeState(new SlimeState.IdleState(this));
                break;
            case state.notice:
                fsm.changeState(new SlimeState.NoticeState(this));
                break;
            case state.attack:
                fsm.changeState(new SlimeState.AttackState(this));
                break;
            case state.die:
                fsm.changeState(new CommonState.DieState(this));
                break;
        }
    }
}
