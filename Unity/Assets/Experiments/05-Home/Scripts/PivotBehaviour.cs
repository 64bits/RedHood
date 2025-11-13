using UnityEngine;

/// <summary>
/// State machine behaviour for Pivot animations
/// Signals the controller when the pivot state is exited.
/// </summary>
public class PivotBehaviour : StateMachineBehaviour
{
    private CharacterLocomotionController controller;
    
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Get the controller reference
        if (controller == null)
        {
            controller = animator.GetComponent<CharacterLocomotionController>();
        }
    }
    
    // OnStateUpdate is no longer needed
    
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Signal the controller that the pivot animation is finished.
        if (controller != null)
        {
            controller.EndPivot();
        }
    }
}