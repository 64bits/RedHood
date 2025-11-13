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
    
    private Animator animator;
    private CharacterController characterController;
    
    private Vector3 currentDirection = Vector3.forward;
    private bool isRunning = false;
    private bool isExecutingTurn = false;
    private float committedAngle = 0f;

    // FIX: Add a 'logicalForward' to act as a stable base for rotation calculations,
    // inspired by your reference script.
    private Vector3 logicalForward;
    
    // Animator parameters
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");
    private static readonly int TurnAngleParam = Animator.StringToHash("turnAngle");
    private static readonly int ExecuteTurnParam = Animator.StringToHash("executeTurn");
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        
        // FIX: Initialize logicalForward to the character's starting direction.
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
        }
    }
    
    /// <summary>
    /// Set the target direction for the character to face/move towards
    /// </summary>
    public void SetTargetDirection(Vector3 direction)
    {
        // FIX: Do not normalize here. We need the magnitude
        // to check for "no input" vs "move input".
        currentDirection = direction; 
        currentDirection.y = 0; // Keep direction horizontal
    }
    
    private void Update()
    {
        // FIX: Check the magnitude of our raw input vector.
        // Use a small deadzone threshold.
        float inputMagnitude = currentDirection.magnitude;
        bool hasInput = shouldMove && inputMagnitude > 0.1f;

        // --- STOPPING LOGIC ---
        // If we have no input, stop running and do nothing else.
        if (!hasInput)
        {
            if (isRunning) // Only update if we need to
            {
                isRunning = false;
                animator.SetBool(IsRunningParam, false);
            }
            return; // Exit Update
        }
        
        // If we're here, we know we have input (hasInput is true).

        // If we are turning, let the turn animation finish.
        if (isExecutingTurn) return;
        
        // FIX: NOW we get the normalized direction, just for rotation calculations.
        Vector3 targetDirection = currentDirection.normalized;
        
        float signedAngle = Vector3.SignedAngle(logicalForward, targetDirection, Vector3.up);
        
        float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
        float halfSnapZone = currentSnapZone * 0.5f;
        
        // Within snap zone - snap rotation
        if (Mathf.Abs(signedAngle) <= halfSnapZone)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                slerpSpeed * Time.deltaTime
            );
            
            logicalForward = transform.forward;
            
            // --- RUNNING LOGIC ---
            // We already know we have input.
            // Use the 'runStartThreshold' to decide if we should START running.
            if (!isRunning && inputMagnitude >= runStartThreshold)
            {
                isRunning = true;
                animator.SetBool(IsRunningParam, true);
            }
        }
        // Outside snap zone - commit to a turn animation
        else
        {
            CommitToTurn(signedAngle);
        }
    }
    
    private void CommitToTurn(float angle)
    {
        Debug.Log("Committing to turn at "+angle);
        isExecutingTurn = true;
        committedAngle = angle;
        
        // Set the angle parameter so animator can choose correct animation
        animator.SetFloat(TurnAngleParam, committedAngle);
        
        // Trigger the turn
        animator.SetTrigger(ExecuteTurnParam);
    }
    
    // Called by Animation Event at end of turn animations
    public void OnTurnComplete()
    {
        isExecutingTurn = false;
        
        // FIX: The turn is over. Snap 'logicalForward' to our new,
        // animation-driven 'transform.forward'. This provides a stable
        // base for the next Update() calculation.
        logicalForward = transform.forward;
    }
    
    private void OnAnimatorMove()
    {
        // Apply root motion position always
        Vector3 movement = animator.deltaPosition;
        characterController.Move(movement);
        
        // FIX: ONLY apply root motion rotation if we are in a committed turn.
        // Otherwise, the Slerp in Update() handles rotation, and we
        // avoid the "double rotation" bug.
        if (isExecutingTurn)
        {
            transform.rotation *= animator.deltaRotation;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // ... (Gizmos code remains the same, it's great for debugging!)
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + currentDirection * 3f);

        // Draw the logicalForward
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + logicalForward * 2f);
        
        if (Application.isPlaying)
        {
            float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
            float halfSnapZone = currentSnapZone * 0.5f;
            
            // FIX: Draw the snap zone relative to the logicalForward
            Vector3 leftBound = Quaternion.Euler(0, -halfSnapZone, 0) * logicalForward * 2f;
            Vector3 rightBound = Quaternion.Euler(0, halfSnapZone, 0) * logicalForward * 2f;
            
            Gizmos.color = isExecutingTurn ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
        }
    }
}