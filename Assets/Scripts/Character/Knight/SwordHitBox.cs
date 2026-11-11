using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    public int damage = 10;

    WeaponAim weaponAim;

    void Start()
    {
        weaponAim = GetComponentInParent<WeaponAim>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        //Demo_Monster monster = other.GetComponentInParent<Demo_Monster>();
        Monster monster = other.GetComponentInParent<Monster>();

        if (monster == null)
            return;

        if (weaponAim == null)
            return;

        if (!weaponAim.CanAttack)
            return;

        monster.TakeDamage(damage);
        weaponAim.ConsumeSwing();
    }
}