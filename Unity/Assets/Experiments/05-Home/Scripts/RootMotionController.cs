using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class RootMotionController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private bool shouldMove = false;
    
    [Header("Snap Zone Settings")]
    [SerializeField] private float idleSnapZone = 15f;
    [SerializeField] private float runningSnapZone = 30f;
    [SerializeField] private float slerpSpeed = 5f;
    
    [Header("State Thresholds")]
    [SerializeField] private float runStartThreshold = 2f;
    [SerializeField] private float runStopThreshold = 0.1f;
    [SerializeField] private float runningInertiaTime = 0.2f; // NEW: Time buffer before stopping
    
    private Animator animator;
    private CharacterController characterController;
    
    private Vector3 currentDirection = Vector3.forward;
    private bool isRunning = false;
    private bool isExecutingTurn = false;
    private float committedAngle = 0f;

    private Vector3 logicalForward;
    
    // NEW: Track when we last had valid running input
    private float lastRunningInputTime = -1f;
    
    // Animator parameters
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");
    private static readonly int TurnAngleParam = Animator.StringToHash("turnAngle");
    private static readonly int ExecuteTurnParam = Animator.StringToHash("executeTurn");
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        
        logicalForward = transform.forward;
    }
    
    /// <summary>
    /// Enable or disable movement
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        shouldMove = enabled;
        
        if (!enabled && isRunning)
        {
            isRunning = false;
            animator.SetBool(IsRunningParam, false);
            lastRunningInputTime = -1f;
        }
    }
    
    /// <summary>
    /// Set the target direction for the character to face/move towards
    /// </summary>
    public void SetTargetDirection(Vector3 direction)
    {
        currentDirection = direction; 
        currentDirection.y = 0;
    }
    
    private void Update()
    {
        float inputMagnitude = currentDirection.magnitude;
        bool hasInput = shouldMove && inputMagnitude > 0.1f;
        
        // FIX: Update the "last time we had input" timestamp
        if (hasInput)
        {
            lastRunningInputTime = Time.time;
        }
        
        // FIX: Only stop running if BOTH conditions are met:
        // 1. Input is below threshold
        // 2. Enough time has passed since last valid input (inertia expired)
        if (isRunning)
        {
            float timeSinceLastInput = Time.time - lastRunningInputTime;
            
            if (inputMagnitude <= runStopThreshold && timeSinceLastInput > runningInertiaTime)
            {
                isRunning = false;
                animator.SetBool(IsRunningParam, false);
                Debug.Log($"Stopped running: no input for {timeSinceLastInput:F2}s");
            }
        }
        
        // No input at all - just return (but character might still be "running" due to inertia)
        if (!hasInput)
        {
            return;
        }
        
        // If we're in a turn animation, let it finish
        if (isExecutingTurn) return;
        
        Vector3 targetDirection = currentDirection.normalized;
        float signedAngle = Vector3.SignedAngle(logicalForward, targetDirection, Vector3.up);
        
        float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
        float halfSnapZone = currentSnapZone * 0.5f;
        
        // Within snap zone - smooth rotation
        if (Mathf.Abs(signedAngle) <= halfSnapZone)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                slerpSpeed * Time.deltaTime
            );
            
            logicalForward = transform.forward;
            
            // Start running only if we're NOT running and input is strong enough
            if (!isRunning && inputMagnitude >= runStartThreshold)
            {
                isRunning = true;
                animator.SetBool(IsRunningParam, true);
                Debug.Log("Started running");
            }
        }
        // Outside snap zone - trigger turn animation
        else
        {
            CommitToTurn(signedAngle);
        }
    }
    
    private void CommitToTurn(float angle)
    {
        Debug.Log($"Committing to turn at {angle:F1}° (isRunning: {isRunning})");
        isExecutingTurn = true;
        committedAngle = angle;
        
        animator.SetFloat(TurnAngleParam, committedAngle);
        animator.SetTrigger(ExecuteTurnParam);
    }
    
    // Called by Animation Event at end of turn animations
    public void OnTurnComplete()
    {
        isExecutingTurn = false;
        logicalForward = transform.forward;

        // Immediately check if we should start running after turn completes
        if (!isRunning && currentDirection.magnitude >= runStartThreshold)
        {
            isRunning = true;
            animator.SetBool(IsRunningParam, true);
            Debug.Log("Started running");
        }
    }
    
    private void OnAnimatorMove()
    {
        Vector3 movement = animator.deltaPosition;
        characterController.Move(movement);
        
        if (isExecutingTurn)
        {
            transform.rotation *= animator.deltaRotation;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + currentDirection * 3f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + logicalForward * 2f);
        
        if (Application.isPlaying)
        {
            float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
            float halfSnapZone = currentSnapZone * 0.5f;
            
            Vector3 leftBound = Quaternion.Euler(0, -halfSnapZone, 0) * logicalForward * 2f;
            Vector3 rightBound = Quaternion.Euler(0, halfSnapZone, 0) * logicalForward * 2f;
            
            Gizmos.color = isExecutingTurn ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
            
            // NEW: Show inertia status
            if (isRunning && currentDirection.magnitude <= runStopThreshold)
            {
                float timeSinceLastInput = Time.time - lastRunningInputTime;
                float inertiaRemaining = runningInertiaTime - timeSinceLastInput;
                if (inertiaRemaining > 0)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
                }
            }
        }
    }
}