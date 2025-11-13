using UnityEngine;

/// <summary>
/// State machine behaviour for the RunLoop state
/// Notifies the controller when the character is in the main running animation.
/// </summary>
public class RunLoopBehaviour : StateMachineBehaviour
{
    private CharacterLocomotionController controller;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (controller == null)
        {
            controller = animator.GetComponent<CharacterLocomotionController>();
        }
        
        if (controller != null)
        {
            controller.SetInRunLoop(true); // <--- Flag the controller that we are running
        }
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (controller != null)
        {
            controller.SetInRunLoop(false); // <--- Unflag when leaving the run loop
        }
    }
}