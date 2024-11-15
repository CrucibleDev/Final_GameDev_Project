using UnityEngine;

[CreateAssetMenu(fileName = "Big Spell", menuName = "Combat/Spells/Big")]
public class BigSpell : Spell
{
    public BigSpell()
    {
        damageModifier = 2f;
        fireRateModifier = 0.5f;
        projectileSpeedModifier = 0.8f;
        cooldownTime = 5f;
    }
    
    public override void OnProjectileSpawn(Projectile projectile)
    {
        projectile.transform.localScale *= 2f;
    }
} 