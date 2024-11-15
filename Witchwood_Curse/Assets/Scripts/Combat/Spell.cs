using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Combat/Spell")]
public class Spell : ScriptableObject
{
    [Header("Info")]
    public string spellName;
    public Sprite icon;
    
    [Header("Modifiers")]
    public float damageModifier = 1f;
    public float fireRateModifier = 1f;
    public float projectileSpeedModifier = 1f;
    
    [Header("Visual Effects")]
    public Color projectileColor = Color.white;
    public GameObject impactEffectPrefab;
    
    [Header("Cooldown")]
    public float cooldownTime = 0f;
    
    // Optional: Special effects
    public virtual void OnProjectileSpawn(Projectile projectile) { }
    public virtual void OnProjectileHit(Projectile projectile, GameObject target) { }
} 