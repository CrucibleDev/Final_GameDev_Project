using UnityEngine;

[CreateAssetMenu(fileName = "Fire Spell", menuName = "Combat/Spells/Fire")]
public class FireSpell : Spell
{
    [Header("Fire Properties")]
    public float burnDamage = 5f;
    public float burnDuration = 3f;
    
    public override void OnProjectileSpawn(Projectile projectile)
    {
        // Add fire particles
        var particles = projectile.GetComponent<ParticleSystem>();
        if (particles)
        {
            var main = particles.main;
            main.startColor = Color.red;
        }
    }
    
    public override void OnProjectileHit(Projectile projectile, GameObject target)
    {
        // Apply burn effect
        var burnable = target.GetComponent<IBurnable>();
        if (burnable != null)
        {
            burnable.ApplyBurn(burnDamage, burnDuration);
        }
    }
} 