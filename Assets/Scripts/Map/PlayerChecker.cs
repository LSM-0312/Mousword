using UnityEngine;
using UnityEngine.Events;

public class PlayerChecker : MonoBehaviour
{
    //1 - 4 스테이지 시작시 이 스크립트 활성화
    //태그가 플레이어인 오브젝트를 감지하면 몬스터 소환
    //감지 기준은 2D 박스 콜라이더 isTrigger 로 감지

    [SerializeField] private bool triggerOnce = true;

    public UnityEvent onPlayerEnter;

    private BoxCollider2D col;
    private bool isTriggered = false;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        if(col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(isTriggered && triggerOnce)
        {
            return;
        }

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;

            onPlayerEnter?.Invoke();

            if (triggerOnce)
            {
                col.enabled = false;
            }
        }
    }
}
