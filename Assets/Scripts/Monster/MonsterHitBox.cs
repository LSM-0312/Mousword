using UnityEngine;

public class MonsterHitBox : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerStatus playerStatus = collision.GetComponent<PlayerStatus>();

            Monster monster = GetComponentInParent<Monster>();

            if (monster == null || playerStatus == null)
            {
                return;
            }
            
            //데미지 전달
            playerStatus.TakeDamage(monster.damage);
        }
    }
}
