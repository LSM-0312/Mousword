using UnityEngine;

public class Knife : MonoBehaviour
{
    public float moveSpeed = 2f;
    private int knifeDirection = 0;
    private Goblin goblin;

    void Update()
    {
        transform.position = new Vector2(transform.position.x + knifeDirection * moveSpeed * Time.deltaTime, transform.position.y);
    }

    public void setSpeed(float speed)
    {
        moveSpeed = speed;
    }
    public void setGoblin(Goblin owner)
    {
        goblin = owner;
    }
    public void setDirection(int direction)
    {
        knifeDirection = direction;
        transform.localScale = new Vector3((direction >= 0 ? -1 : 1), 1, 1);
    }

    void OnBecameInvisible()
    {
        if (goblin != null)
        {
            goblin.isAttack = false;
        }
        Destroy(gameObject);
    }
    
}
