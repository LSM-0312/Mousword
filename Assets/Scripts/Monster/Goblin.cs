using UnityEngine;

public class Goblin : Monster
{
    protected Rigidbody2D rigid;              // 2D 물리 연산을 다루기 위한 컴포넌트 변수

    public float maxIdleMoveDistance = 3f;         //고블린이 플레이어를 인식하지 않을 때 이동할 수 있는 최대 거리
    [HideInInspector] public Vector2 startPosition;                                   //고블린의 처음 위치
    [HideInInspector] public float curMoveDistance = 0f;                              //고블린이 이동한 거리
    public float noticeMoveDistance = 1f;                                             //고블린이 플레이어를 인식했을때 매번 이동하는 거리
    [HideInInspector] public int moveDirection = 1;                                   //고블린이 이동할 방향(1: 오른쪽, -1: 왼쪽)

    [HideInInspector] public bool isGround;                   //고블린이 이동할 때 바로앞의 땅을 감지하는 변수
    private float checkRadius = 0.2f;       //고블린이 이동할 때 바로앞의 땅을 감지하는 반지름

    public GameObject Knife;
    public Transform knifeSpawnPoint;
    [HideInInspector] public bool isAttack = false; //고블린 검이 화면에 남아있음

    protected override void Start()
    {
        base.Start();
        rigid = GetComponent<Rigidbody2D>();

        constMoveSpeed = moveSpeed;                  //고블린의 이동속도 상수화
        startPosition = transform.position;          //고블린의 처음 위치 저장

        fsm = new FSM(new GoblinState.IdleState(this));
    }

    protected override void Update()
    {
        base.Update();
        if (!isGround) //앞에 땅이 없으면
        {
            isWait = true; //대기상태 갱신
        }

        anim.SetBool("Idle", isWait); //대기 애니메이션 상태 전환
        fsm.stateUpdate();
    }

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

        rigid.linearVelocity = new Vector2(moveDirection * moveSpeed, rigid.linearVelocity.y);
    }

    public override void changeState(state newState)
    {
        switch (newState)
        {
            case state.idle:
                fsm.changeState(new GoblinState.IdleState(this));
                break;
            case state.notice:
                fsm.changeState(new GoblinState.NoticeState(this));
                break;
            case state.attack:
                fsm.changeState(new GoblinState.AttackState(this));
                break;
            case state.die:
                fsm.changeState(new CommonState.DieState(this));
                break;
        }
    }

    public void Attack(float speed)
    {
        if (Knife != null && knifeSpawnPoint != null)
        {
            GameObject knife = Instantiate(Knife, knifeSpawnPoint.position, Quaternion.identity);

            // 생성된 칼의 투명도를 1(불투명)로 변경
            GameObject Body = knife.transform.Find("KnifeBody").gameObject;
            SpriteRenderer sr = Body.GetComponent<SpriteRenderer>();
            Color color = sr.color;
            color.a = 1.0f;
            sr.color = color;

            MonsterHitBox hitBox = knife.GetComponentInChildren<MonsterHitBox>();
            if (hitBox != null)
            {
                hitBox.SetMonster(this);
            }

            knife.GetComponent<Knife>().setSpeed(speed);
            knife.GetComponent<Knife>().setDirection(moveDirection);
            knife.GetComponent<Knife>().setGoblin(this);
        }
    }
}
