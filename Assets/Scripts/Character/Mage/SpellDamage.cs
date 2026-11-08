using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 피해 적용 로직 — 연출과 완전히 분리된 순수 게임 로직.
///
/// 플레이어 주문뿐 아니라 몬스터 공격·함정·투사체 등 어디서든 재사용한다.
/// MonoBehaviour 가 아니라 static 클래스이므로 오브젝트에 붙이지 않고 바로 호출한다.
///   예) SpellDamage.ApplyArea(point, 2f, 30f, enemyLayer);
///
/// "찾기(Find)"와 "적용(Apply)"을 나눠 둔 이유:
///   피해 없이 대상만 필요한 효과(둔화·스턴·넉백 등)가 Find 만 재사용할 수 있도록.
/// </summary>
public static class SpellDamage
{
    /// <summary>지점에서 가장 가까운 몬스터 1마리를 찾는다. 없으면 null.</summary>
    public static Demo_Monster FindNearest(Vector2 point, float radius, LayerMask layer)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius, layer);

        Demo_Monster nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            // 콜라이더가 자식에 있어도 부모의 몬스터를 찾아낸다
            Demo_Monster monster = hit.GetComponentInParent<Demo_Monster>();
            if (monster == null) continue;

            float dist = ((Vector2)monster.transform.position - point).sqrMagnitude;
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = monster;
            }
        }

        return nearest;
    }

    /// <summary>반경 안의 모든 몬스터를 찾는다 (중복 제거됨).</summary>
    public static List<Demo_Monster> FindAll(Vector2 point, float radius, LayerMask layer)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(point, radius, layer);

        // 몬스터 하나가 콜라이더를 여러 개 가진 경우 중복 방지
        HashSet<Demo_Monster> unique = new HashSet<Demo_Monster>();
        List<Demo_Monster> result = new List<Demo_Monster>();

        foreach (Collider2D hit in hits)
        {
            Demo_Monster monster = hit.GetComponentInParent<Demo_Monster>();
            if (monster == null) continue;

            if (unique.Add(monster))
                result.Add(monster);
        }

        return result;
    }

    /// <summary>가장 가까운 1마리에게 피해. 맞은 대상을 반환(없으면 null).</summary>
    public static Demo_Monster ApplySingle(Vector2 point, float radius, float damage, LayerMask layer)
    {
        Demo_Monster target = FindNearest(point, radius, layer);

        if (target != null)
            target.TakeDamage(damage);

        return target;
    }

    /// <summary>반경 안 전체에 피해. 맞은 대상 수를 반환.</summary>
    public static int ApplyArea(Vector2 point, float radius, float damage, LayerMask layer)
    {
        List<Demo_Monster> targets = FindAll(point, radius, layer);

        foreach (Demo_Monster target in targets)
            target.TakeDamage(damage);

        return targets.Count;
    }
}
