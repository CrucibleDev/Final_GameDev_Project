using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float ignoreCollisionTime = 0.2f; // Time to ignore collisions
    
    private float damage;
    private Vector3 velocity;
    private SpellSlot[] activeSpells;
    private Rigidbody rb;
    private GameObject shooter;
    private float spawnTime;
    
    public void Initialize(float damage, Vector3 velocity, SpellSlot[] spells, GameObject shooter = null)
    {
        this.damage = damage;
        this.velocity = velocity;
        this.activeSpells = spells;
        this.shooter = shooter;
        this.spawnTime = Time.time;
        
        rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Ignore collisions for a brief period after spawning
        if (Time.time - spawnTime < ignoreCollisionTime) return;
        
        // Ignore collisions with the shooter
        if (collision.gameObject == shooter) return;
        
        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}
 