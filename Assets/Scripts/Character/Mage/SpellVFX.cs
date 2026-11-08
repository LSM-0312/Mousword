using UnityEngine;

/// <summary>
/// 주문 연출 오브젝트 — 보여주는 일만 한다. 피해는 절대 주지 않는다.
///
/// "피해의 주인은 하나" 규칙:
///   피해는 SpellCaster 가 SpellDamage 를 통해 전담한다.
///   이 프리팹에 피해를 주는 스크립트를 함께 붙이면 데미지가 두 번 들어가므로 주의.
///   (예전 SpellAoE 는 스스로 피해를 줬으므로 이 프리팹에 붙이면 안 됨)
///
/// 붙이는 곳: 이펙트 프리팹 루트 (파티클 / 스프라이트 애니메이션과 함께).
/// </summary>
public class SpellVFX : MonoBehaviour
{
    [Tooltip("이 시간(초) 뒤 자동 제거. 연출 길이보다 살짝 길게. 0 이하면 자동 제거 안 함.")]
    [SerializeField] float lifetime = 1f;

    [Tooltip("켜면 소환 시 회전을 초기화 (부모/시전 방향의 영향을 받지 않음).")]
    [SerializeField] bool keepUpright = true;

    void Start()
    {
        if (keepUpright)
            transform.rotation = Quaternion.identity;

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }
}
