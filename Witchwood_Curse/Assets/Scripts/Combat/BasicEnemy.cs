using UnityEngine;

public class BasicEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float health = 50f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float damage = 5f;

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    
    private float nextFireTime;
    private Transform player;
    private Material material;
    private Color originalColor;

    void Start()
    {
        // Find the player
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Get material for damage flash
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    void Update()
    {
        if (player == null) return;
        
        // Look at player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep it flat on XZ plane
        transform.rotation = Quaternion.LookRotation(directionToPlayer);

        // Shoot if we can
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Vector3 direction = (player.position - firePoint.position).normalized;
        direction.y = 0; // Keep it flat
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        var projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.Initialize(damage, direction * projectileSpeed, null);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        StartCoroutine(FlashRoutine());

        if (health <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        material.color = originalColor;
    }

    private void Die()
    {
        // Add effects, drop items, etc. here
        Destroy(gameObject);
    }
} 