using UnityEngine;

public class Knife : MonoBehaviour
{
    public float moveSpeed = 2f;

    private int knifeDirection = 0;

    void Update()
    {
        if (!isOutOfScreen())
        {
            transform.position = new Vector2(transform.position.x + knifeDirection * moveSpeed * Time.deltaTime, transform.position.y);
        }
        else
        {
            GoblinState.AttackState.isAttack = false;
            Destroy(gameObject);
        }
    }

    public void setSpeed(float speed)
    {
        moveSpeed = speed;
    }
    public void setDirection(int direction)
    {
        knifeDirection = direction;
    }
    public bool isOutOfScreen()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        return screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height;
    }
}
