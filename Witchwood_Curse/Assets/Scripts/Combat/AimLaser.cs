using UnityEngine;

public class AimLaser : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform firePoint;

    [Header("Settings")]
    [SerializeField] private float maxLength = 15f;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private float lineWidth = 0.05f;

    private void Start()
    {
        // Create line renderer if not assigned
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Create or find firePoint if not assigned
        if (firePoint == null)
        {
            // First try to find an existing firePoint in parent
            firePoint = transform.parent?.Find("FirePoint");
            
            // If still null, create a new one
            if (firePoint == null)
            {
                GameObject firePointObj = new GameObject("FirePoint");
                firePoint = firePointObj.transform;
                firePoint.SetParent(transform);
                firePoint.localPosition = Vector3.forward * 0.5f; // Slightly in front
            }
        }

        // Configure line renderer
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
    }

    private void Update()
    {
        // Safety check
        if (lineRenderer == null || firePoint == null)
        {
            Debug.LogError("AimLaser: Missing required components. Disabling script.", this);
            enabled = false;
            return;
        }

        Vector3 direction = transform.forward;
        RaycastHit hit;
        
        // Set start position
        lineRenderer.SetPosition(0, firePoint.position);

        // Check if ray hits something
        if (Physics.Raycast(firePoint.position, direction, out hit, maxLength))
        {
            // Set end position to hit point
            lineRenderer.SetPosition(1, hit.point);

            // Change color if hitting enemy
            bool isEnemy = hit.collider.GetComponent<IDamageable>() != null;
            lineRenderer.startColor = lineRenderer.endColor = isEnemy ? enemyColor : normalColor;
        }
        else
        {
            // No hit, extend to max length
            lineRenderer.SetPosition(1, firePoint.position + (direction * maxLength));
            lineRenderer.startColor = lineRenderer.endColor = normalColor;
        }
    }

    // Optional: Show the laser direction in the editor
    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(firePoint.position, firePoint.position + transform.forward * maxLength);
        }
    }
} 