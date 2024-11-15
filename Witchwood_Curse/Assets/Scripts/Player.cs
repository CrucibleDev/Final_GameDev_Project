using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    private Game gm;
    private Rigidbody rb;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;
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

    [Header("Camera Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float dashFOV = 70f;
    [SerializeField] private float normalCameraDistance = 8f;
    [SerializeField] private float dashCameraDistance = 7f;
    [SerializeField] private float cameraLerpSpeed = 10f;
    [SerializeField] private float normalCameraOffset = 1f;
    [SerializeField] private float dashCameraOffset = 2f;
    [SerializeField] private float dashCameraScale = 0.875f;
    private Vector3 initialCameraLocalPos;
    private float currentMaxOffset;
    private Vector3 currentCameraOffset = Vector3.zero;
    private float currentFOV;
    private Vector3 currentCameraPos;
    private Quaternion normalCameraRot;

    private int currentHealth;
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

    void Start()
    {
        playerCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        gm = Game.instance;
        currentHealth = maxHealth;
        
        rb.drag = groundDrag;
        rb.angularDrag = groundDrag;
        
        // Store initial camera offset
        if (playerCamera != null)
        {
            initialCameraLocalPos = playerCamera.transform.localPosition;
            currentCameraPos = initialCameraLocalPos;
            playerCamera.fieldOfView = normalFOV;
        }
        currentMaxOffset = normalCameraOffset;
        currentFOV = normalFOV;
        normalCameraRot = Quaternion.Euler(45f, -45f, 0f);
        
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = normalFOV;
            playerCamera.transform.localPosition = initialCameraLocalPos;
            playerCamera.transform.localRotation = normalCameraRot;
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

    void LateUpdate()
    {
        if (!isAlive || playerCamera == null) return;
        
        // Calculate scale factor for camera position
        float scale = isDashing ? dashCameraScale : 1f;
        
        // Scale all dimensions to maintain the angle
        Vector3 targetPos = initialCameraLocalPos * scale;
        
        // Smoothly transition FOV and position based on dash state
        float targetFOV = isDashing ? dashFOV : normalFOV;
        float targetOffset = isDashing ? dashCameraOffset : normalCameraOffset;
        
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * cameraLerpSpeed);
        currentCameraPos = Vector3.Lerp(currentCameraPos, targetPos, Time.deltaTime * cameraLerpSpeed);
        currentMaxOffset = Mathf.Lerp(currentMaxOffset, targetOffset, Time.deltaTime * cameraLerpSpeed);
        
        // Calculate camera offset based on player velocity
        Vector3 targetOffsetVector = -rb.velocity * 0.1f;
        targetOffsetVector = Vector3.ClampMagnitude(targetOffsetVector, currentMaxOffset);
        
        // Smoothly move camera to new offset
        currentCameraOffset = Vector3.Lerp(currentCameraOffset, targetOffsetVector, Time.deltaTime * cameraLerpSpeed);
        
        // Apply position and offset
        playerCamera.transform.localPosition = currentCameraPos + currentCameraOffset;
        playerCamera.fieldOfView = currentFOV;
    }
}
