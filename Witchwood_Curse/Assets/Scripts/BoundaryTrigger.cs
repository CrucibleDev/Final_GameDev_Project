using UnityEngine;

public class BoundaryTrigger : MonoBehaviour
{
    private static int playerBoundaryCount = 0;
    private static Vector3 lastValidPosition;
    private static bool isInitialized = false;
    
    private static Rigidbody playerRb;
    private static CharacterController playerController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerBoundaryCount++;
            Debug.Log($"Enter boundary. Count: {playerBoundaryCount}");
            
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
                
                Vector3 boundaryCenter = transform.position;
                Vector3 directionToPlayer = (other.transform.position - boundaryCenter).normalized;
                float boundaryRadius;

                if (TryGetComponent<CapsuleCollider>(out var capsuleCollider))
                {
                    boundaryRadius = capsuleCollider.radius;
                }
                else if (TryGetComponent<BoxCollider>(out var boxCollider))
                {
                    boundaryRadius = boxCollider.size.x * 0.5f;
                    directionToPlayer = transform.InverseTransformDirection(directionToPlayer);
                    directionToPlayer.z = 0;
                    directionToPlayer = transform.TransformDirection(directionToPlayer.normalized);
                }
                else
                {
                    Debug.LogError("No supported collider found on boundary!");
                    return;
                }
                
                Vector3 clampedPosition = boundaryCenter + directionToPlayer * boundaryRadius;
                clampedPosition.y = other.transform.position.y;
                
                Debug.Log($"Clamping position from {other.transform.position} to {clampedPosition}");
                
                if (playerController != null)
                {
                    playerController.enabled = false;
                    other.transform.position = clampedPosition;
                    playerController.enabled = true;
                }
                else if (playerRb != null)
                {
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