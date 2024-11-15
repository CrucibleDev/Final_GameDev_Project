using UnityEngine;

public enum CameraLockMode { Rotational, Planar }

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraLockPoint;

    [Header("Camera Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float dashFOV = 70f;
    [SerializeField] private float dashCameraScale = 0.875f;
    [SerializeField] private float cameraLerpSpeed = 10f;
    [SerializeField] private float normalCameraOffset = 1f;
    [SerializeField] private float dashCameraOffset = 2f;
    [SerializeField] private bool useSemiLockedCamera = false;
    [SerializeField] private float swayAmount = 2f;
    [SerializeField] private float swaySpeed = 3f;
    [SerializeField] private CameraLockMode lockMode = CameraLockMode.Rotational;
    [SerializeField] private float planarFollowSpeed = 2f;  // How quickly camera shifts on plane
    [SerializeField] private float smoothTime = 0.1f;

    private Vector3 initialCameraPos;
    private Quaternion initialCameraRot;
    private float currentFOV;
    private float currentMaxOffset;
    private Vector3 currentCameraOffset = Vector3.zero;
    private Vector3 currentCameraPos;
    private Vector3 lockedCameraPosition;
    private Vector3 initialOffset;
    private Vector3 cameraVelocity = Vector3.zero;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Store initial camera setup
        initialCameraPos = playerCamera.transform.position;
        initialOffset = initialCameraPos - player.transform.position;
        initialCameraRot = Quaternion.Euler(45f, -45f, 0f);
        currentCameraPos = initialCameraPos;
        currentFOV = normalFOV;
        currentMaxOffset = normalCameraOffset;
        
        // Setup camera
        playerCamera.transform.parent = null;
        playerCamera.fieldOfView = normalFOV;
        playerCamera.transform.position = initialCameraPos;
        playerCamera.transform.rotation = initialCameraRot;
        
        if (useSemiLockedCamera && cameraLockPoint != null)
        {
            lockedCameraPosition = cameraLockPoint.position;
        }
    }

    void LateUpdate()
    {
        if (player == null || playerCamera == null || !player.IsAlive) return;
        
        if (useSemiLockedCamera)
        {
            HandleSemiLockedCamera();
        }
        else
        {
            HandleFollowCamera();
        }
        
        playerCamera.fieldOfView = currentFOV;
    }

    void HandleSemiLockedCamera()
    {
        if (cameraLockPoint == null) return;
        
        switch (lockMode)
        {
            case CameraLockMode.Rotational:
                // Original rotational behavior
                Vector3 rotationalVelocityOffset = Vector3.ClampMagnitude(player.Velocity * 0.2f, swayAmount);
                rotationalVelocityOffset.y = 0;
                
                Vector3 rotationalTargetPos = cameraLockPoint.position + rotationalVelocityOffset;
                
                playerCamera.transform.position = Vector3.Lerp(
                    playerCamera.transform.position, 
                    rotationalTargetPos, 
                    Time.deltaTime * swaySpeed
                );
                playerCamera.transform.LookAt(player.transform.position);
                break;
                
            case CameraLockMode.Planar:
                // Calculate direction from lock point to player
                Vector3 directionToPlayer = player.transform.position - cameraLockPoint.position;
                directionToPlayer.y = 0; // Keep it on the horizontal plane
                
                // Calculate how far the camera should shift based on player distance
                float distanceToPlayer = directionToPlayer.magnitude;
                float shiftAmount = Mathf.Min(distanceToPlayer * 0.5f, swayAmount * 2f);
                Vector3 shiftDirection = directionToPlayer.normalized;
                
                // Calculate velocity-based sway
                Vector3 velocityOffset = Vector3.ClampMagnitude(player.Velocity * 0.2f, swayAmount * 0.5f);
                velocityOffset.y = 0;
                
                // Calculate target position
                Vector3 basePos = cameraLockPoint.position + (shiftDirection * shiftAmount);
                Vector3 targetPos = basePos + velocityOffset;
                targetPos.y = cameraLockPoint.position.y; // Maintain height
                
                // Smooth movement
                playerCamera.transform.position = Vector3.Lerp(
                    playerCamera.transform.position,
                    targetPos,
                    Time.deltaTime * planarFollowSpeed
                );
                
                // Maintain fixed rotation
                playerCamera.transform.rotation = initialCameraRot;
                break;
        }
    }

    void HandleFollowCamera()
    {
        // Calculate target position
        Vector3 targetPos = player.transform.position + initialOffset;
        
        if (player.IsDashing)
        {
            Vector3 dashOffset = initialOffset * dashCameraScale;
            targetPos = player.transform.position + dashOffset;
        }
        
        // Smooth FOV changes
        currentFOV = Mathf.Lerp(currentFOV, player.IsDashing ? dashFOV : normalFOV, Time.deltaTime * cameraLerpSpeed);
        
        // Use SmoothDamp for more stable following
        playerCamera.transform.position = Vector3.SmoothDamp(
            playerCamera.transform.position,
            targetPos,
            ref cameraVelocity,
            smoothTime
        );
        
        // Maintain rotation
        playerCamera.transform.rotation = initialCameraRot;
    }

    public void SetCameraMode(bool semiLocked)
    {
        if (playerCamera == null) return;
        
        useSemiLockedCamera = semiLocked;
        
        if (semiLocked && cameraLockPoint != null)
        {
            lockedCameraPosition = cameraLockPoint.position;
        }
        else
        {
            // Reset to follow camera position
            currentCameraPos = player.transform.position + (initialCameraPos - player.transform.position);
            playerCamera.transform.position = currentCameraPos;
            playerCamera.transform.rotation = initialCameraRot;
        }
    }

    public void SetLockMode(CameraLockMode newMode)
    {
        lockMode = newMode;
    }

    public void ToggleLockMode()
    {
        lockMode = lockMode == CameraLockMode.Rotational ? 
            CameraLockMode.Planar : CameraLockMode.Rotational;
    }
} 