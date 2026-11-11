using UnityEngine;
using System.Collections;

public class PlayerStatus : MonoBehaviour
{
    // 생존 스텟
    [SerializeField] private float maxHp = 100f;       // 최대 체력
    [SerializeField] private float currentHp;          //현재 체력

    [SerializeField] private float maxMp = 50f;        // 최대 마나
    [SerializeField] private float currentMp;          // 현재 마나

    //방어 스텟
    [SerializeField] private float defense = 5f;       // 방어력(데미지 감소)
    [SerializeField] private float invincibleTime = 0.5f; // 피격 무적 시간

    private bool isInvincible = false;                 // 현재 무적 상태 여부
    private SpriteRenderer spriteRenderer;

    // 다른 스크립트에서 스텟을 안전하게 읽을 수 있도록 프로퍼티(Property) 제공
    public float CurrentHp => currentHp;
    public float MaxHp => maxHp;
    public float CurrentMp => currentMp;
    public float MaxMp => maxMp;
    public bool IsDead { get; private set; } = false;   // 사망 여부

    void Start()
    {
        // 게임 시작 시 체력과 마나를 최대치로 초기화
        currentHp = maxHp;
        currentMp = maxMp;

        // 피격 무적 연출을 위한 스프라이트 렌더러 컴포넌트
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 플레이어가 데미지를 입을 때 호출하는 메서드
    public void TakeDamage(float damage)
    {
        // 사망했거나 무적 상태라면 데미지를 받지 않음
        if (IsDead || isInvincible) return;

        // 방어력을 계산한 최종 데미지 (최소 1의 데미지는 입도록 설정)
        float finalDamage = Mathf.Max(damage - defense, 1f);

        currentHp -= finalDamage;
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp); // 체력이 0 미만, 최대치 초과되지 않게 방지

        Debug.Log($"플레이어가 {finalDamage}의 데미지를 입었습니다. 남은 체력: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
        else
        {
            // 무적 시간 및 피격 연출 연출 시작
            StartCoroutine(InvincibleCooldown());
        }
    }

    // 피격 시 일정 시간 무적 상태로 만들고 깜빡이는 연출
    private IEnumerator InvincibleCooldown()
    {
        // 무적 상태로 전환
        isInvincible = true;

        // 무적 시간 동안 캐릭터를 깜빡거리게 만듦 (피격 시각 효과)
        float timer = 0f;
        while (timer < invincibleTime)
        {
            // 알파값을 0.2와 1.0 사이로 변환
            float alpha = spriteRenderer.color.a == 1f ? 0.2f : 1f;
            spriteRenderer.color = new Color(1f, 1f, 1f, alpha);

            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // 무적 종료 후 원래 투명도로 복귀
        spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
        isInvincible = false;       // 무적 상태 해제
    }

    // 마나 / 스태미나 소비 메서드
    public bool ConsumeMp(float amount)
    {
        if (currentMp >= amount)
        {
            currentMp -= amount;
            return true; // 소비 성공
        }

        Debug.Log("마나/스태미나가 부족합니다.");
        return false; // 소비 실패
    }

    // 마나  회복 메서드 (포션이나 자연 회복용)
    public void RecoverMp(float amount)
    {
        currentMp += amount;
        currentMp = Mathf.Clamp(currentMp, 0f, maxMp);
    }

    // 체력 회복 메서드 (흡혈이나 포션용)
    public void RecoverHp(float amount)
    {
        if (IsDead) return;

        currentHp += amount;
        currentHp = Mathf.Clamp(currentHp, 0f, maxHp);
    }

    // 플레이어 사망 처리
    private void Die()
    {
        IsDead = true;
        Debug.Log("플레이어가 사망했습니다.");

        // TODO: 애니메이터에게 사망 애니메이션 전달, 조작 스크립트 비활성화 등의 로직을 여기에 추가
        // 예: GetComponent<PlayerMove>().enabled = false;
    }
}
