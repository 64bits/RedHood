using UnityEngine;
using UnityEngine.InputSystem;

public class CrouchController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    
    [Header("Input Settings")]
    [SerializeField] private InputActionReference crouchAction;

    [Header("Animation Parameters")]
    [SerializeField] private string crouchBoolName = "isCrouching";
    [SerializeField] private string crouchTriggerName = "crouch";

    private void OnEnable()
    {
        // Enable the input action and subscribe to both pressed and released events
        crouchAction.action.Enable();
        crouchAction.action.started += OnCrouchStarted;
        crouchAction.action.canceled += OnCrouchCanceled;
    }

    private void OnDisable()
    {
        // Unsubscribe from events and disable the input action
        crouchAction.action.started -= OnCrouchStarted;
        crouchAction.action.canceled -= OnCrouchCanceled;
        crouchAction.action.Disable();
    }

    private void OnCrouchStarted(InputAction.CallbackContext context)
    {
        // Start crouching when key is pressed
        if (animator != null)
        {
            animator.SetTrigger(crouchTriggerName);
            animator.SetBool(crouchBoolName, true);
        }
    }

    private void OnCrouchCanceled(InputAction.CallbackContext context)
    {
        // Stop crouching when key is released
        if (animator != null)
        {
            animator.SetBool(crouchBoolName, false);
        }
    }

    // Optional: Validate that references are set
    private void OnValidate()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }
}