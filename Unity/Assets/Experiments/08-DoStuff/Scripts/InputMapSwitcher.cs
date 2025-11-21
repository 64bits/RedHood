using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.Cinemachine;

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
/// 4. The "Player" map must have actions named "Inventory" and "Map".
/// 5. The "UI" map must have an action named "Cancel" (or "Escape").
/// </summary>
[RequireComponent(typeof(PlayerInput))]
public class InputMapSwitcher : MonoBehaviour
{
    // Static events for UI mode (inventory)
    public static event Action OnEnterUIMode;
    public static event Action OnExitUIMode;
    
    // Static events for Map mode
    public static event Action OnEnterMapMode;
    public static event Action OnExitMapMode;

    private string playerMapName = "Player";
    private string uiMapName = "UI";

    [Header("Component References")]
    [Tooltip("The CinemachineInputAxisController component. This will be disabled when in UI or Map mode.")]
    [SerializeField] private CinemachineInputAxisController cinemachineAxisController;

    private PlayerInput playerInput;
    
    private enum InputMode { Player, UI, Map }
    private InputMode currentMode = InputMode.Player;

    /// <summary>
    /// Gets whether the game is currently in UI mode (e.g., inventory is open).
    /// </summary>
    public bool IsInUI => currentMode == InputMode.UI;
    
    /// <summary>
    /// Gets whether the game is currently in Map mode.
    /// </summary>
    public bool IsInMap => currentMode == InputMode.Map;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (cinemachineAxisController == null)
        {
            cinemachineAxisController = GetComponent<CinemachineInputAxisController>();
        }

        if (cinemachineAxisController == null)
        {
            Debug.LogWarning("CinemachineInputAxisController not found. Camera controls will not be disabled in UI/Map mode.", this);
        }
    }

    private void Start()
    {
        SwitchToPlayerMap();
    }

    // --- Message Handlers (Called by PlayerInput) ---

    public void OnInventory()
    {
        if (currentMode == InputMode.Player)
        {
            SwitchToUIMap();
        }
    }

    public void OnMap()
    {
        if (currentMode == InputMode.Player)
        {
            SwitchToMapMode();
        }
        else if (currentMode == InputMode.Map)
        {
            SwitchToPlayerMap();
        }
    }

    public void OnCancel()
    {
        if (currentMode != InputMode.Player)
        {
            SwitchToPlayerMap();
        }
    }

    // --- Public Control Methods ---

    public void SwitchToUIMap()
    {
        if (currentMode == InputMode.UI) return;

        playerInput.SwitchCurrentActionMap(uiMapName);
        currentMode = InputMode.UI;

        if (cinemachineAxisController != null)
        {
            cinemachineAxisController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnEnterUIMode?.Invoke();
    }

    public void SwitchToMapMode()
    {
        if (currentMode == InputMode.Map) return;

        playerInput.SwitchCurrentActionMap(uiMapName);
        currentMode = InputMode.Map;

        if (cinemachineAxisController != null)
        {
            cinemachineAxisController.enabled = false;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnEnterMapMode?.Invoke();
    }

    public void SwitchToPlayerMap()
    {
        if (currentMode == InputMode.Player && playerInput.currentActionMap?.name == playerMapName)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        InputMode previousMode = currentMode;
        
        playerInput.SwitchCurrentActionMap(playerMapName);
        currentMode = InputMode.Player;

        if (cinemachineAxisController != null)
        {
            cinemachineAxisController.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Fire the appropriate exit event based on what mode we were in
        if (previousMode == InputMode.UI)
        {
            OnExitUIMode?.Invoke();
        }
        else if (previousMode == InputMode.Map)
        {
            OnExitMapMode?.Invoke();
        }
    }
}