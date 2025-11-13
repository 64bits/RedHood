using UnityEngine;

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