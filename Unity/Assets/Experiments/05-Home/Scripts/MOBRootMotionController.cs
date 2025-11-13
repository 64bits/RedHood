using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class MOBRootMotionController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 1.5f;
    
    [Header("Snap Zone Settings")]
    [SerializeField] private float idleSnapZone = 15f; // Total angle (7.5 degrees each side)
    [SerializeField] private float runningSnapZone = 30f; // Total angle (15 degrees each side)
    [SerializeField] private float slerpSpeed = 5f;
    
    [Header("State Thresholds")]
    [SerializeField] private float runStartDistance = 2f; // Must be outside stop distance to start running
    
    private Animator animator;
    private CharacterController characterController;
    
    private bool isRunning = false;
    private static readonly int AngleParam = Animator.StringToHash("angle");
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Calculate direction and angle to target
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0; // Keep movement on horizontal plane
        float distanceToTarget = directionToTarget.magnitude;
        
        // Check if we should stop (reached target)
        if (distanceToTarget <= stopDistance)
        {
            if (isRunning)
            {
                isRunning = false;
                animator.SetBool(IsRunningParam, false);
            }
            animator.SetFloat(AngleParam, 0f);
            return;
        }
        
        // Calculate signed angle between forward and target direction
        Vector3 targetDirection = directionToTarget.normalized;
        float signedAngle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        
        // Update animator with current angle
        animator.SetFloat(AngleParam, signedAngle);
        
        // Determine current snap zone based on state
        float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
        float halfSnapZone = currentSnapZone * 0.5f;
        
        // Handle rotation when within snap zone
        if (Mathf.Abs(signedAngle) <= halfSnapZone)
        {
            // Smoothly rotate towards target
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                slerpSpeed * Time.deltaTime
            );
            
            // Start running if we're idle and far enough from target
            if (!isRunning && distanceToTarget > runStartDistance)
            {
                isRunning = true;
                animator.SetBool(IsRunningParam, true);
            }
        }
        else
        {
            // Outside snap zone - let root motion handle rotation via pivot animations
            // The animator will select appropriate turn animation based on angle parameter
        }
    }
    
    private void OnAnimatorMove()
    {
        // Apply root motion rotation
        transform.rotation *= animator.deltaRotation;
        
        // Apply root motion position through character controller
        Vector3 movement = animator.deltaPosition;
        characterController.Move(movement);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        
        // Draw stop distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, stopDistance);
        
        // Draw run start distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, runStartDistance);
        
        // Draw direction line
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, target.position);
        
        // Draw snap zone visualization
        if (Application.isPlaying)
        {
            float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
            float halfSnapZone = currentSnapZone * 0.5f;
            
            Vector3 leftBound = Quaternion.Euler(0, -halfSnapZone, 0) * transform.forward * 2f;
            Vector3 rightBound = Quaternion.Euler(0, halfSnapZone, 0) * transform.forward * 2f;
            
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
        }
    }
}