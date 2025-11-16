using UnityEngine;

public class SquirrelController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float runSpeed = 3f;
    
    private Animator animator;
    private bool isRunning = false;
    private Vector3 runDirection;
    private float runTimer = 0f;
    private Transform playerTransform;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // Ensure animator exists
        if (animator == null)
        {
            Debug.LogWarning("Animator component not found on squirrel!");
        }
    }

    void Update()
    {
        // Handle running behavior
        if (isRunning)
        {
            runTimer -= Time.deltaTime;
            
            if (runTimer > 0f)
            {
                // Move away from player in X-Z plane only
                Vector3 horizontalMove = new Vector3(runDirection.x, 0f, runDirection.z) * runSpeed * Time.deltaTime;
                transform.position += horizontalMove;
            }
            else
            {
                // Stop running
                StopRunning();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if player entered trigger and we're not already running
        if (other.CompareTag("Player") && !isRunning)
        {
            StartRunning(other.transform);
        }
    }

    void StartRunning(Transform player)
    {
        playerTransform = player;
        isRunning = true;
        runTimer = 3f; // Run for 3 seconds
        
        // Calculate direction away from player in X-Z plane only
        Vector3 directionToPlayer = player.position - transform.position;
        Vector3 horizontalDirection = new Vector3(directionToPlayer.x, 0f, directionToPlayer.z);
        runDirection = -horizontalDirection.normalized;
        
        // Make squirrel face the direction it's running (X-Z plane only)
        if (runDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(runDirection);
        }
        
        // Set animator parameter
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
        }
    }

    void StopRunning()
    {
        isRunning = false;
        runTimer = 0f;
        playerTransform = null;
        
        // Reset animator parameter
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }
    }
}