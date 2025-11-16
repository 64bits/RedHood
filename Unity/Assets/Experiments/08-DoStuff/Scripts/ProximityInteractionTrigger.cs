using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(InteractableObject))]
[RequireComponent(typeof(Collider))]
public class ProximityInteractionTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    
    private InteractableObject interactable;
    private Collider triggerCollider;
    private bool playerInRange;
    private InputAction interactAction;

    private void Awake()
    {
        interactable = GetComponent<InteractableObject>();
        triggerCollider = GetComponent<Collider>();
        
        // Ensure the collider is set as a trigger
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: Collider is not set as trigger. Setting isTrigger to true.");
            triggerCollider.isTrigger = true;
        }
        
        // Set up the input action for the Interact button
        interactAction = new InputAction(binding: "<Keyboard>/e"); // Default to E key
        interactAction.Enable();
    }

    private void Update()
    {
        // Check for interact input when player is in range
        if (playerInRange && interactAction.WasPressedThisFrame())
        {
            interactable.Interact();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            interactable.ShowIndicator();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            interactable.HideIndicator();
        }
    }

    private void OnDisable()
    {
        if (playerInRange)
        {
            playerInRange = false;
            interactable.HideIndicator();
        }
    }

    private void OnDestroy()
    {
        interactAction?.Disable();
        interactAction?.Dispose();
    }
}