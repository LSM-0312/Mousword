using UnityEngine;

public class Falling_Rock: MonoBehaviour
{
    public float damage = 30f;        //데미지

    private bool isTarget = false;        //플레이어가 낙하 범위 내에 있는지 확인
    private GameObject RangeBox;                         //낙하 범위안에 들어있는지 확인하는 오브젝트

    public Transform groundCheck;              // 발밑 위치를 정할 오브젝트
    private bool isGround;                     //바로 아래에 땅을 감지하는 변수
    public Transform player;

    private Rigidbody2D rigid;

    void Start()
    {
        // 초기화 작업 수행
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f; // 중력 미작용
        RangeBox = GameObject.Find("Range");
        RangeBox.SetActive(false);
    }

    void Update()
    {
        //플레이어가 낙하 범위 내에 있는지 확인
        isTarget = player.localPosition.x >= transform.localPosition.x - RangeBox.transform.localScale.x / 2 &&
                   player.localPosition.x < transform.localPosition.x + RangeBox.transform.localScale.x / 2 &&
                   player.localPosition.y >= transform.localPosition.y - RangeBox.transform.localScale.y;

        isGround = Physics2D.OverlapCircle(groundCheck.position, 0.2f, LayerMask.GetMask("Ground"));

        //땅에 닿으면 오브젝트 제거
        if (isGround)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        //플레이어가 낙하범위에 있는 지 확인하고 중력 작용
        if (isTarget)
        {
           rigid.gravityScale = 3f; // 중력 작용
        }
    }

    //플레이어와 충돌 시 데미지 전달
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Debug.Log("충돌");
            PlayerStatus playerStatus = collision.GetComponent<PlayerStatus>();

            //데미지 전달
            playerStatus.TakeDamage(damage);
        }
    }
}

