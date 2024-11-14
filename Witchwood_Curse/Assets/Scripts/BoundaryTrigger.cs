using UnityEngine;

public class BoundaryTrigger : MonoBehaviour
{
    private static int playerBoundaryCount = 0;
    private static Vector3 lastValidPosition;
    private static bool isInitialized = false;
    
    // Cache the player's components
    private static Rigidbody playerRb;
    private static CharacterController playerController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerBoundaryCount++;
            Debug.Log($"Enter boundary. Count: {playerBoundaryCount}");
            
            // Cache components if not already done
            if (!isInitialized)
            {
                playerRb = other.GetComponent<Rigidbody>();
                playerController = other.GetComponent<CharacterController>();
                lastValidPosition = other.transform.position;
                isInitialized = true;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lastValidPosition = other.transform.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerBoundaryCount--;
            Debug.Log($"Exit boundary. Count: {playerBoundaryCount}");

            if (playerBoundaryCount <= 0)
            {
                playerBoundaryCount = 0;
                
                // Get the direction from the boundary center to the player
                Vector3 boundaryCenter = transform.position;
                Vector3 directionToPlayer = (other.transform.position - boundaryCenter).normalized;
                
                // Project the last valid position onto the boundary surface
                float boundaryRadius = GetComponent<CapsuleCollider>().radius;
                Vector3 clampedPosition = boundaryCenter + directionToPlayer * boundaryRadius;
                clampedPosition.y = other.transform.position.y; // Maintain player's height
                
                Debug.Log($"Clamping position from {other.transform.position} to {clampedPosition}");
                
                // Force position reset based on component type
                if (playerController != null)
                {
                    playerController.enabled = false;
                    other.transform.position = clampedPosition;
                    playerController.enabled = true;
                }
                else if (playerRb != null)
                {
                    // Add force to push player back into boundary
                    Vector3 pushDirection = (boundaryCenter - other.transform.position).normalized;
                    playerRb.velocity = Vector3.zero;
                    playerRb.angularVelocity = Vector3.zero;
                    playerRb.MovePosition(clampedPosition);
                    playerRb.AddForce(pushDirection * 10f, ForceMode.Impulse);
                }
                else
                {
                    other.transform.position = clampedPosition;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            playerBoundaryCount = 0;
            isInitialized = false;
            playerRb = null;
            playerController = null;
        }
    }
} 