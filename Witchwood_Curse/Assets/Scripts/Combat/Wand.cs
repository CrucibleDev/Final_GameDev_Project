using UnityEngine;

public class Wand : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] private float baseFireRate = 0.5f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float baseProjectileSpeed = 20f;
    [SerializeField] private GameObject baseProjectilePrefab;
    [SerializeField] private Transform firePoint;
    
    [Header("Spell Slots")]
    [SerializeField] private SpellSlot[] spellSlots = new SpellSlot[3];
    
    private float nextFireTime;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))  // Left click to fire
        {
            TryShoot();
        }
        
        // Rotate wand to face mouse on XZ plane
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - transform.position;
            direction.y = 0; // Keep it flat
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    void TryShoot()
    {
        if (Time.time < nextFireTime) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, transform.position);
        
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 targetPoint = ray.GetPoint(distance);
            Vector3 direction = targetPoint - firePoint.position;
            direction.y = 0; // Keep it flat
            Fire(direction.normalized);
        }
    }
    
    public void Fire(Vector3 direction)
    {
        // Calculate modified stats from active spells
        float modifiedDamage = baseDamage;
        float modifiedFireRate = baseFireRate;
        float modifiedSpeed = baseProjectileSpeed;
        
        // Apply spell modifiers
        foreach (var slot in spellSlots)
        {
            if (slot != null && slot.CurrentSpell != null)
            {
                modifiedDamage *= slot.CurrentSpell.damageModifier;
                modifiedFireRate *= slot.CurrentSpell.fireRateModifier;
                modifiedSpeed *= slot.CurrentSpell.projectileSpeedModifier;
            }
        }
        
        // Create projectile
        GameObject projectile = Instantiate(baseProjectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        var projectileComponent = projectile.GetComponent<Projectile>();
        projectileComponent.Initialize(modifiedDamage, direction * modifiedSpeed, spellSlots);
        
        nextFireTime = Time.time + modifiedFireRate;
    }
} 