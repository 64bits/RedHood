using UnityEngine;

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