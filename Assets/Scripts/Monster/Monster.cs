using UnityEngine;
using UnityEngine.UI;

public abstract class Monster : MonoBehaviour
{
    [SerializeField] protected Slider HPSlider;
    [SerializeField] protected float maxHP = 100f;
    public float currentHP;
    public float moveSpeed = 4f;      //이동속도
    [HideInInspector] public float constMoveSpeed = 0f;                        //슬라임의 이동속도 상수화
    public float damage = 10f;        //공격력

    public Transform player;
    public float recognizeRadius = 8f;          //몬스터가 플레이어를 인식하는 범위 반지름
    public float attackRadius = 4f;               //몬스터가 플레이어를 공격하는 범위
    protected bool isRecognzed = false;                             //몬스터가 플레이어를 인식했는지 확인
    [HideInInspector] public float distanceToPlayer;                                  //몬스터와 플레이어 사이의 거리
    public enum state{idle, notice, attack, die}                            //몬스터의 상태
    protected state curState;                                       //현재 몬스터의 상태

    public Transform groundCheck;                                   // 발밑 위치를 정할 오브젝트
    [SerializeField] private GameObject monsterAttackHitbox;                         //몬스터 공격 동작의 공격 범위 오브젝트


    [HideInInspector] public bool isWait = false;                                     //몬스터가 가만히 대기하는 상태
    public float maxWaitTime = 2f;                 //몬스터가 가만히 대기하는 최대 시간(초)
    [HideInInspector] public float curWaitTime = 0f;                                  //몬스터가 가만히 대기한 시간(초)

    [HideInInspector] public Animator anim;                                // 애니메이션 파라미터 제어를 위한 변수

    [HideInInspector] public bool isAnimeEnd = true;                        //애니메이션이 끝났는지 확인하는 변수
    protected FSM fsm;                                   //상태 전환을 위한 FSM 객체


    protected virtual void Start()
    {
        currentHP = maxHP;
        anim = GetComponent<Animator>();

        if (HPSlider != null)
        {
            HPSlider.maxValue = maxHP;
            HPSlider.value = currentHP;
        }

        if (monsterAttackHitbox != null)
        {
            monsterAttackHitbox.SetActive(false); //몬스터 공격 동작의 공격 범위 비활성화
        }
        
    }

    protected virtual void Update()
    {
        //몬스터와 플레이어 거리 계산
        distanceToPlayer = Vector2.Distance(transform.position, player.position);

        //플레이어와의 거리가 인식 반지름 이하인지 확인
        isRecognzed = distanceToPlayer <= recognizeRadius;

        //몬스터 인식 상태 전환
        setState(); 
    }

    protected virtual void setState()
    {
        if (isAnimeEnd)
        {
            //플레이어 인식 여부에 따라 상태 전환
            if (isRecognzed)
            {
                if (distanceToPlayer <= attackRadius)                             //플레이어가 공격 범위 안에 있을 때 공격 범위 활성화
                {
                    curState = state.attack;
                }
                else
                {
                    curState = state.notice;
                }
            }
            else
            {
                curState = state.idle;
            }

            changeState(curState);
        }
    }


    public void TakeDamage(float damage)
    {
        currentHP -= damage;

        if (HPSlider != null)
        {
            HPSlider.value = currentHP;
        }

        if (currentHP <= 0)
        {
            curState = state.die;
            changeState(curState);
            Destroy(gameObject, 1.0f);
        }
    }

    public void flip(int direction)
    {
        transform.localScale = new Vector3(direction, 1, 1);
    }

    protected bool groundChecker(float radius)
    {
        // 핵심: 발밑에 가상의 원을 그려서 'Ground' 레이어와 닿아있는지 체크
        bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, radius, LayerMask.GetMask("Ground"));
        return isGrounded;
    }

    public void setAttackHitbox(int on)
    {
        bool isOn = on>0 ? true : false;
        monsterAttackHitbox.SetActive(isOn);
    }

    public virtual void changeState(state newState)
    {
        curState = newState;

        switch (curState)
        {
            case state.die:
                fsm.changeState(new CommonState.DieState(this));
                break;
        }
    }
    private void animeClosing(int on)
    {
        isAnimeEnd = on==1;

        if (anim != null && anim.GetCurrentAnimatorClipInfo(0).Length > 0)
        {
            string clipName = anim.GetCurrentAnimatorClipInfo(0)[0].clip.name;
        }
    }
}
