using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class MOBRootMotionController : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float stopDistance = 1.5f;
    
    [Header("Snap Zone Settings")]
    [SerializeField] private float idleSnapZone = 15f;
    [SerializeField] private float runningSnapZone = 30f;
    [SerializeField] private float slerpSpeed = 5f;
    
    [Header("State Thresholds")]
    [SerializeField] private float runStartDistance = 2f;
    
    private Animator animator;
    private CharacterController characterController;
    
    private bool isRunning = false;
    private bool isExecutingTurn = false;
    private float committedAngle = 0f;
    
    // Animator parameters
    private static readonly int IsRunningParam = Animator.StringToHash("isRunning");
    private static readonly int TurnAngleParam = Animator.StringToHash("turnAngle");
    private static readonly int ExecuteTurnParam = Animator.StringToHash("executeTurn");
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }
    
    private void Update()
    {
        if (target == null) return;
        
        // Don't update direction while executing a committed turn
        // if (isExecutingTurn) return;
        
        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0;
        float distanceToTarget = directionToTarget.magnitude;
        
        // Check if we should stop
        if (distanceToTarget <= stopDistance)
        {
            if (isRunning)
            {
                isRunning = false;
                animator.SetBool(IsRunningParam, false);
            }
            return;
        }
        
        Vector3 targetDirection = directionToTarget.normalized;
        float signedAngle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);
        
        float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
        float halfSnapZone = currentSnapZone * 0.5f;
        Debug.Log(Mathf.Abs(signedAngle));
        // Within snap zone - smooth rotation
        if (Mathf.Abs(signedAngle) <= halfSnapZone)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, 
                targetRotation, 
                slerpSpeed * Time.deltaTime
            );
            
            // Start running if idle and far enough
            if (!isRunning && distanceToTarget > runStartDistance)
            {
                isRunning = true;
                animator.SetBool(IsRunningParam, true);
            }
        }
        // Outside snap zone - commit to a turn animation if not already
        else
        {
            if (isExecutingTurn) return;
            CommitToTurn(signedAngle);
        }
    }
    
    private void CommitToTurn(float angle)
    {
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
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(target.position, stopDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(target.position, runStartDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, target.position);
        
        if (Application.isPlaying)
        {
            float currentSnapZone = isRunning ? runningSnapZone : idleSnapZone;
            float halfSnapZone = currentSnapZone * 0.5f;
            
            Vector3 leftBound = Quaternion.Euler(0, -halfSnapZone, 0) * transform.forward * 2f;
            Vector3 rightBound = Quaternion.Euler(0, halfSnapZone, 0) * transform.forward * 2f;
            
            Gizmos.color = isExecutingTurn ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, transform.position + leftBound);
            Gizmos.DrawLine(transform.position, transform.position + rightBound);
        }
    }
}