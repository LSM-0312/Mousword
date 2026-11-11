using UnityEngine;

public class SwordWave : MonoBehaviour
{
    private float speed;
    private float damage;
    private Rigidbody2D rb;

    public void Initialize(float moveSpeed, float skillDamage)
    {
        speed = moveSpeed;
        damage = skillDamage;
        rb = GetComponent<Rigidbody2D>();

        // 생성 당시 설정된 회전 각도의 오른쪽(transform.right) 방향으로 날아감
        if (rb != null)
        {
            rb.linearVelocity = transform.right * speed;
        }

        // 3초 뒤 메모리 관리를 위해 자동 파괴
        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터와 충돌 시 데미지 부여 및 검기 삭제
        Slime monster = other.GetComponentInParent<Slime>();
        if (monster != null)
        {
            monster.TakeDamage((int)damage);
            Destroy(gameObject);
        }
    }
}
