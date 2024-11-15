using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    private Game gm;
    private Rigidbody rb;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float range = 10f;
    [SerializeField] private float spread = 1f;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 35f;
    [SerializeField] private float maxVelocity = 8f;
    [SerializeField] private float groundDrag = 6f;
    [SerializeField] private float airDrag = 1f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float deceleration = 8f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 50f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 1f;

    private Vector3 inputVector = Vector3.zero;
    private Vector3 moveDirection = Vector3.zero;
    private bool isAlive = true;
    private bool isDebugSet = true;

    // Dash variables
    private bool isDashing = false;
    private float dashTimeLeft = 0f;
    private float dashCooldownTimer = 0f;

    private bool isMoving = false;
    private Vector3 lastMoveDirection;

    // Add these properties to expose necessary information to the camera
    public bool IsAlive => isAlive;
    public bool IsDashing => isDashing;
    public Vector3 Velocity => rb.velocity;

    [SerializeField] private PlayerCamera cameraController;

    [Header("UI References")]
    [SerializeField] private HealthBar healthBar;

    void Start()
    {
        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        gm = Game.instance;
        
        // Cap current health at max health
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.UpdateHealth(currentHealth);
        }
    }
    
    void Update()
    {
        if (!isAlive) return;
        HandleInputs();
        UpdateDashCooldown();
    }

    void FixedUpdate()
    {
        if (!isAlive) return;
        
        isAlive = currentHealth > 0;

        if (isDebugSet)
        {
            Debug.DrawRay(transform.position + Vector3.up * 0.5f, moveDirection * 5, Color.green);
        }

        HandleMovement();
    }

    void HandleMovement()
    {
        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleNormalMovement();
        }
    }

    void HandleDash()
    {
        dashTimeLeft -= Time.fixedDeltaTime;
        
        if (dashTimeLeft <= 0)
        {
            isDashing = false;
            rb.drag = groundDrag;
            return;
        }

        rb.drag = airDrag;
        
        float forceMagnitude = dashForce / dashDuration * Time.fixedDeltaTime;
        rb.AddForce(moveDirection * forceMagnitude, ForceMode.Impulse);
    }

    void HandleNormalMovement()
    {
        isMoving = moveDirection.magnitude > 0.1f;

        if (isMoving)
        {
            float currentSpeed = rb.velocity.magnitude;
            float accelerationMultiplier = Mathf.Lerp(acceleration, 1f, currentSpeed / maxVelocity);
            
            rb.AddForce(moveDirection * moveSpeed * accelerationMultiplier, ForceMode.Force);
            lastMoveDirection = moveDirection;
        }
        else if (rb.velocity.magnitude > 0.1f)
        {
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(-horizontalVelocity.normalized * deceleration * rb.mass, ForceMode.Force);
        }

        Vector3 horizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        if (horizontalVel.magnitude > maxVelocity)
        {
            Vector3 clampedVel = horizontalVel.normalized * maxVelocity;
            rb.velocity = new Vector3(clampedVel.x, rb.velocity.y, clampedVel.z);
        }

        rb.drag = isMoving ? groundDrag : groundDrag * 1.5f;
    }

    void HandleInputs()
    {
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        inputVector = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) inputVector.z = 1f;
        if (Input.GetKey(KeyCode.S)) inputVector.z = -1f;
        if (Input.GetKey(KeyCode.D)) inputVector.x = 1f;
        if (Input.GetKey(KeyCode.A)) inputVector.x = -1f;

        inputVector.Normalize();
        moveDirection = forward * inputVector.z + right * inputVector.x;

        if (Input.GetKeyDown(KeyCode.LeftShift) && moveDirection != Vector3.zero && dashCooldownTimer <= 0)
        {
            InitiateDash();
        }
    }

    void InitiateDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    void UpdateDashCooldown()
    {
        if (dashCooldownTimer > 0)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }

    // Method to change camera mode
    public void SetCameraLockMode(CameraLockMode mode)
    {
        if (cameraController != null)
        {
            cameraController.SetLockMode(mode);
        }
    }
    
    // Method to toggle camera mode
    public void ToggleCameraLockMode()
    {
        if (cameraController != null)
        {
            cameraController.ToggleLockMode();
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        healthBar.UpdateHealth(currentHealth);
        
        if (currentHealth <= 0)
        {
            isAlive = false;
        }
    }

    // Add this to update the health bar whenever health changes
    public void SetHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth);
        }
    }

#if UNITY_EDITOR
    // This will update the health bar in the editor when you change currentHealth
    private void OnValidate()
    {
        if (healthBar != null && Application.isPlaying)
        {
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            healthBar.UpdateHealth(currentHealth);
        }
    }
#endif
}
