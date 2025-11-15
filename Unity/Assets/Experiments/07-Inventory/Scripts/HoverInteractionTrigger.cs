using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(InteractableObject))]
[RequireComponent(typeof(Collider))]
public class HoverInteractionTrigger : MonoBehaviour
{
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float maxRayDistance = 100f;
    
    private InteractableObject interactable;
    private Camera mainCam;
    private bool isHovering;
    private Mouse mouse;
    
    private void Awake()
    {
        interactable = GetComponent<InteractableObject>();
        mainCam = Camera.main;
        mouse = Mouse.current;
    }
    
    private void Update()
    {
        if (mouse == null || mainCam == null) return;
        
        Vector2 mousePos = mouse.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePos);
        
        bool hitThisObject = false;
        
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastLayers))
        {
            if (hit.collider.gameObject == gameObject)
            {
                hitThisObject = true;
                
                if (!isHovering)
                {
                    isHovering = true;
                    interactable.ShowIndicator();
                }
                
                // Check for click
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
            }
        }
        
        if (!hitThisObject && isHovering)
        {
            isHovering = false;
            interactable.HideIndicator();
        }
    }
    
    private void OnDisable()
    {
        if (isHovering)
        {
            isHovering = false;
            interactable.HideIndicator();
        }
    }
}
