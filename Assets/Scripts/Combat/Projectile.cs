using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;  // How long the projectile lives
    
    private float damage;
    private Vector3 velocity;
    private SpellSlot[] activeSpells;
    private Rigidbody rb;
    
    public void Initialize(float damage, Vector3 velocity, SpellSlot[] spells)
    {
        this.damage = damage;
        this.velocity = velocity;
        this.activeSpells = spells;
        
        rb = GetComponent<Rigidbody>();
        rb.velocity = velocity;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        
        // Destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        var damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }
        
        Destroy(gameObject);
    }
}
 