using UnityEngine;
using UnityEngine.InputSystem;
// Updated to use the correct Cinemachine namespace
using Unity.Cinemachine; // Required for Cinemachine access

/// <summary>
/// Handles switching between "Player" and "UI" action maps.
/// Disables Cinemachine input when the "UI" map is active.
/// 
/// REQUIREMENTS:
/// 1. This script must be on the same GameObject as your 'PlayerInput' component.
/// 2. The 'PlayerInput' component's "Behavior" must be set to "Send Messages".
/// 3. Your InputActions asset must have two Action Maps:
///    - One named "Player".
///    - One named "UI".
/// 4. The "Player" map must have an action named "Inventory".
/// 5. The "UI" map must have an action named "Cancel" (or "Escape").
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InputMapSwitcher : MonoBehaviour
{
    // --- Simplified Fields ---
    private string playerMapName = "Player";
    private string uiMapName = "UI";

    [Header("Component References")]
    
    [Tooltip("The CinemachineInputAxisController component, usually on the same GameObject. This will be disabled when in UI mode.")]
    // Changed type from CinemachineInputProvider to CinemachineInputAxisController
    [SerializeField] private CinemachineInputAxisController cinemachineAxisController;

    private PlayerInput playerInput;

    /// <summary>
    /// Gets whether the game is currently in UI mode (e.g., inventory is open).
    /// </summary>
    public bool IsInUI { get; private set; }

    private void Awake()
    {
        // Get the PlayerInput component on this GameObject
        playerInput = GetComponent<PlayerInput>();
        // Try to find the CinemachineInputAxisController if it wasn't assigned in the inspector
        if (cinemachineAxisController == null)
        {
            // Updated to get CinemachineInputAxisController
            cinemachineAxisController = GetComponent<CinemachineInputAxisController>();
        }

        // Log a warning if it's still not found, as camera look won't be disabled.
        if (cinemachineAxisController == null)
        {
            Debug.LogWarning("CinemachineInputAxisController not found. Camera controls will not be disabled in UI mode.", this);
        }
    }

    private void Start()
    {
        // Ensure we start in the correct state: Player controls active.
        SwitchToPlayerMap();
    }

    // --- Message Handlers (Called by PlayerInput) ---

    /// <summary>
    /// This method is called by the PlayerInput component (using Send Messages)
    /// when the "Inventory" action in the "Player" map is performed.
    /// 
    /// FIX: Removed (InputAction.CallbackContext context) to satisfy Send Messages.
    /// </summary>
    public void OnInventory()
    {
        // Since Send Messages only calls on 'performed', we only need the IsInUI check.
        if (!IsInUI)
        {
            SwitchToUIMap();
        }
    }

    /// <summary>
    /// This method is called by the PlayerInput component (using Send Messages)
    /// when the "Cancel" action (or your "Escape" action) in the "UI" map is performed.
    /// 
    /// FIX: Removed (InputAction.CallbackContext context) to satisfy Send Messages.
    /// </summary>
    public void OnCancel()
    {
        if (IsInUI)
        {
            SwitchToPlayerMap();
        }
    }

    // --- Public Control Methods ---

    /// <summary>
    /// Activates the UI map, frees the cursor,
    /// and disables Cinemachine camera controls.
    /// </summary>
    public void SwitchToUIMap()
    {
        if (IsInUI) return; // Already in UI mode

        playerInput.SwitchCurrentActionMap(uiMapName);
        IsInUI = true;

        // Disable Cinemachine input
        if (cinemachineAxisController != null)
        {
            // Updated to disable the Axis Controller
            cinemachineAxisController.enabled = false;
        }

        // Unlock and show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Activates the Player map, locks the cursor,
    /// and re-enables Cinemachine camera controls.
    /// </summary>
    public void SwitchToPlayerMap()
    {
        // Check if we are already in player mode. (This check also runs on Start)
        // This check is simplified as we no longer care about the inventory panel state.
        if (!IsInUI && playerInput.currentActionMap != null && playerInput.currentActionMap.name == playerMapName)
        {
             // Exception for Start(): ensure cursor is locked if we're not in UI
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        playerInput.SwitchCurrentActionMap(playerMapName);
        IsInUI = false;

        // Re-enable Cinemachine input
        if (cinemachineAxisController != null)
        {
            // Updated to re-enable the Axis Controller
            cinemachineAxisController.enabled = true;
        }

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}