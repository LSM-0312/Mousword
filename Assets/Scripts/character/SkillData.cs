using UnityEngine;

// 유니티 프로젝트 창에서 마우스 우클릭으로 스킬 파일을 만들 수 있게 된다.
// 메뉴를 통해 스킬 데이터 에셋 파일을 새로 만들 수 있도록 등록해주는 속성
[CreateAssetMenu(fileName = "SkillData", menuName = "Scriptable Objects/SkillData")]

// 게임 오브젝트에 컴포넌트로 부착되는 스크립트가 아닌 프로젝트 창에 독립적인 데이터 파일로 존재
public class SkillData : ScriptableObject
{
    public string skillName;       // 스킬 이름
    public float damageMultiplier; // 데미지 배율
    public float manaCost;         // 소모 마나 [cite: 7]
    public float coolTime;         // 쿨타임
    public GameObject effectPrefab;// 스킬 이펙트 프리팹

    public float damage = 10f;

    // 대시 관련 스텟
    public float dashForce = 20f;     // 대시 속도
    public float dashDuration = 0.2f; // 대시 지속 시간

    // 검기 관련 스텟
    public GameObject projectilePrefab; // 날아갈 검기 프리팹
    public float projectileSpeed = 15f;  // 검기 날아가는 속도

    // 궁극기 관련 스텟
    public float hitInterval = 0.08f; // 몬스터 간 피격 지연 시간 (초)
    public GameObject ultimateHitEffectPrefab; // 몬스터 위치에 터질 피격 이펙트

    // 360도 회전 스킬
    public float spinDegrees = 360f;       // 회전 각도
    public float spinDuration = 0.5f;      // 회전 시간
}
