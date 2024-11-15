using UnityEngine;
using System.Collections;

public class MobileEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float health = 50f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float damage = 5f;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float positionReachThreshold = 0.5f;
    
    [Header("Movement")]
    [SerializeField] private float moveRadius = 10f;  // How far from start position the enemy can move
    [SerializeField] private float minMoveDelay = 1f; // Minimum time to wait at each position
    [SerializeField] private float maxMoveDelay = 3f; // Maximum time to wait at each position

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    
    private float nextFireTime;
    private Transform player;
    private Material material;
    private Color originalColor;
    private Vector3 targetPosition;
    private Vector3 startPosition;
    private bool isMoving = false;
    private float moveDelayTimer;

    void Start()
    {
        // Find the player
        player = GameObject.FindGameObjectWithTag("Player").transform;
        
        // Get material for damage flash
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
        
        // Store starting position
        startPosition = transform.position;
        
        // Get first position
        SetNewTargetPosition();
    }

    void Update()
    {
        if (player == null) return;
        
        // Look at player regardless of movement
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Keep it flat on XZ plane
        transform.rotation = Quaternion.LookRotation(directionToPlayer);

        if (isMoving)
        {
            // Move towards target position
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            moveDirection.y = 0; // Keep it flat
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            // Check if we've reached the target
            if (Vector3.Distance(transform.position, targetPosition) < positionReachThreshold)
            {
                isMoving = false;
                moveDelayTimer = Random.Range(minMoveDelay, maxMoveDelay);
                
                // Shoot when reaching position
                if (Time.time >= nextFireTime)
                {
                    Shoot();
                    nextFireTime = Time.time + fireRate;
                }
            }
        }
        else
        {
            // Wait at current position
            moveDelayTimer -= Time.deltaTime;
            if (moveDelayTimer <= 0)
            {
                SetNewTargetPosition();
                isMoving = true;
            }
            
            // Can still shoot while waiting
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void SetNewTargetPosition()
    {
        // Get random point within moveRadius of start position
        Vector2 randomCircle = Random.insideUnitCircle * moveRadius;
        targetPosition = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }

    void Shoot()
    {
        Vector3 direction = (player.position - firePoint.position).normalized;
        direction.y = 0; // Keep it flat
        
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        var projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.Initialize(damage, direction * projectileSpeed, null, gameObject);
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

    private IEnumerator FlashRoutine()
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

    // Optional: Visualize the move radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, moveRadius);
    }
} 