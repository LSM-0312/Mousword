using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 즉발형 광역 폭발 — 소환되는 즉시 반경 내 모든 대상에 피해를 주고, 연출이 끝나면 사라진다.
///
/// 검처럼 콜라이더 트리거로 판정하지 않고 OverlapCircleAll 로 한 번에 훑는 이유:
///  - 광역기는 여러 마리를 "동시에" 때려야 함
///    (SwordHitBox 의 OnTriggerStay2D 방식은 프레임당 한 마리만 처리되는 구조)
///  - 폭발은 지속 접촉이 아니라 순간 판정이므로 물리 콜라이더가 아예 필요 없음
///
/// 붙이는 곳: 폭발 프리팹 루트 (파티클/애니메이션과 함께).
/// </summary>
public class SpellAoE : MonoBehaviour
{
    [Header("피해")]
    [SerializeField] float damage = 25f;
    [Tooltip("폭발 반경. 씬 뷰에서 기즈모로 확인 가능.")]
    [SerializeField] float radius = 1.5f;
    [Tooltip("피해를 줄 대상 레이어. 비워두면(Nothing) 아무도 맞지 않으니 반드시 지정할 것.")]
    [SerializeField] LayerMask targetLayer;

    [Header("수명")]
    [Tooltip("연출이 끝나고 오브젝트가 제거되기까지의 시간(초).")]
    [SerializeField] float lifetime = 1f;

    /// <summary>조준점이 범위 원을 그릴 때 참조 (SpellCaster 경유).</summary>
    public float Radius => radius;

    void Start()
    {
        Explode();
        Destroy(gameObject, lifetime);   // 피해는 즉시, 파괴는 연출이 끝난 뒤
    }

    void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);

        // 몬스터 하나가 콜라이더를 여러 개 가진 경우 중복 피해 방지
        HashSet<Demo_Monster> damaged = new HashSet<Demo_Monster>();

        foreach (Collider2D hit in hits)
        {
            Demo_Monster monster = hit.GetComponentInParent<Demo_Monster>();

            if (monster == null)
                continue;

            // Add 가 false = 이미 이번 폭발에서 맞은 대상
            if (!damaged.Add(monster))
                continue;

            monster.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
