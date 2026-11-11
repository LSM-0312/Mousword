using UnityEngine;

public class MonsterHitBox : MonoBehaviour
{
    private Monster monster;

    private void Awake()
    {
        // 고블린의 자식으로 붙어 있는 히트박스인 경우 부모에서 자동으로 찾음
        monster = GetComponentInParent<Monster>();
    }

    // 복제된 투사체(칼)처럼 분리된 오브젝트에 몬스터 정보를 직접 전달하는 함수
    public void SetMonster(Monster owner)
    {
        monster = owner;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStatus playerStatus = collision.GetComponent<PlayerStatus>();

            if (monster == null || playerStatus == null)
            {
                return;
            }
            
            //데미지 전달
            playerStatus.TakeDamage(monster.damage);
        }
    }
}
