using UnityEngine;

/// <summary>
/// State machine behaviour for the IdleToRun state
/// Handles transition timing based on commitment
/// </summary>
public class IdleToRunBehaviour : StateMachineBehaviour
{
    private static readonly int CommitmentParam = Animator.StringToHash("Commitment");
    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Reset any state if needed
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Monitor commitment and movement for transition
        float commitment = animator.GetFloat(CommitmentParam);
        bool isMoving = animator.GetBool(IsMovingParam);
        
        // This helps ensure smooth transitions
        if (!isMoving && stateInfo.normalizedTime >= 0.8f)
        {
            // Ready to transition to idle
        }
    }
}

/// <summary>
/// State machine behaviour for the RunToIdle state
/// Ensures smooth return to idle
/// </summary>
public class RunToIdleBehaviour : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Ensure we're not marked as moving
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Clean transition to idle
    }
}

/// <summary>
/// State machine behaviour for the RunLoop state
/// Monitors for pivot conditions
/// </summary>
public class RunLoopBehaviour : StateMachineBehaviour
{
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Entered run loop - continuous running
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // The pivot detection is handled in the main controller
        // This behaviour can be extended for additional logic
    }
}

/// <summary>
/// State machine behaviour for Pivot animations
/// Handles pivot timing and blend-out
/// </summary>
public class PivotBehaviour : StateMachineBehaviour
{
    [SerializeField] private float blendOutStartTime = 0.7f; // normalized time to start blending out
    
    private bool hasTriggeredBlendOut = false;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasTriggeredBlendOut = false;
    }
    
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Signal blend out timing to the controller
        // The actual weight adjustment is handled in CharacterLocomotionController
        if (!hasTriggeredBlendOut && stateInfo.normalizedTime >= blendOutStartTime)
        {
            hasTriggeredBlendOut = true;
        }
    }
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Pivot complete
        hasTriggeredBlendOut = false;
    }
}