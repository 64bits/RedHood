using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

/// <summary>
/// Manages a docking station where players can interact to enter a docked UI state.
/// Subscribes to the Interact action from PlayerInput to trigger docking.
/// 
/// SETUP:
/// 1. Attach to a GameObject with a trigger Collider.
/// 2. Assign the InputMapSwitcher reference (or it will auto-find).
/// 3. Assign the prompt canvas (Canvas A) - shown when player is nearby.
/// 4. Assign the docked canvas (Canvas B) - children enabled when docked.
/// 5. Assign the Cinemachine virtual camera to activate when docked.
/// 6. Ensure your Player action map has an "Interact" action.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DockingManager : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    
    [Header("Input")]
    [Tooltip("Reference to the InputMapSwitcher. Will auto-find if not set.")]
    [SerializeField] private InputMapSwitcher inputMapSwitcher;
    
    [Header("Canvases")]
    [Tooltip("World-space canvas shown when player is in proximity (e.g., 'Press E to dock')")]
    [SerializeField] private Canvas promptCanvas;
    
    [Tooltip("World-space canvas whose children are enabled when docked")]
    [SerializeField] private Canvas dockedCanvas;
    
    [Header("Camera")]
    [Tooltip("Virtual camera to enable when docked")]
    [SerializeField] private CinemachineCamera dockedCamera;
    
    [Tooltip("Priority boost for the docked camera when active")]
    [SerializeField] private int dockedCameraPriorityBoost = 10;
    
    [Header("Events")]
    public UnityEvent onPlayerEnterProximity;
    public UnityEvent onPlayerExitProximity;
    public UnityEvent onDocked;
    public UnityEvent onUndocked;
    
    // State
    private bool playerInRange;
    private bool isDocked;
    private int originalCameraPriority;
    private Collider triggerCollider;
    private InputAction interactAction;
    
    /// <summary>
    /// Returns true if the player is currently docked at this station.
    /// </summary>
    public bool IsDocked => isDocked;
    
    /// <summary>
    /// Returns true if the player is in range to dock.
    /// </summary>
    public bool IsPlayerInRange => playerInRange;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        
        if (!triggerCollider.isTrigger)
        {
            Debug.LogWarning($"{gameObject.name}: Collider is not set as trigger. Setting isTrigger to true.");
            triggerCollider.isTrigger = true;
        }
        
        if (dockedCamera != null)
        {
            originalCameraPriority = dockedCamera.Priority.Value;
        }
    }

    private void Start()
    {
        // Auto-find InputMapSwitcher if not assigned
        if (inputMapSwitcher == null)
        {
            inputMapSwitcher = FindFirstObjectByType<InputMapSwitcher>();
        }
        
        if (inputMapSwitcher == null)
        {
            Debug.LogError($"{gameObject.name}: InputMapSwitcher not found. Docking will not work.", this);
            return;
        }
        
        // Get the Interact action from PlayerInput
        var playerInput = inputMapSwitcher.PlayerInput;
        if (playerInput != null)
        {
            interactAction = playerInput.actions?.FindAction("Interact");
            
            if (interactAction == null)
            {
                Debug.LogError($"{gameObject.name}: 'Interact' action not found in PlayerInput actions.", this);
            }
        }
        
        // Initialize canvases to correct state
        SetPromptCanvasVisible(false);
        SetDockedCanvasChildrenActive(false);
        
        if (dockedCamera != null)
        {
            dockedCamera.Priority = originalCameraPriority;
        }
    }

    private void OnEnable()
    {
        // Subscribe to Dock mode exit to handle forced undocking (e.g., Cancel pressed)
        InputMapSwitcher.OnExitDockMode += HandleDockModeClosed;
    }

    private void OnDisable()
    {
        InputMapSwitcher.OnExitDockMode -= HandleDockModeClosed;
        
        // Clean up state if disabled while docked
        if (isDocked)
        {
            ForceUndock();
        }
    }

    private void Update()
    {
        // Check for interact input when player is in range and not already docked
        if (playerInRange && !isDocked && interactAction != null && interactAction.WasPressedThisFrame())
        {
            Dock();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (isDocked) return;
        
        playerInRange = true;
        SetPromptCanvasVisible(true);
        onPlayerEnterProximity?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        playerInRange = false;
        
        // Only hide prompt if not docked
        if (!isDocked)
        {
            SetPromptCanvasVisible(false);
            onPlayerExitProximity?.Invoke();
        }
    }

    /// <summary>
    /// Enters the docked state.
    /// </summary>
    public void Dock()
    {
        if (isDocked) return;
        
        isDocked = true;
        
        // Hide prompt, show docked UI
        SetPromptCanvasVisible(false);
        SetDockedCanvasChildrenActive(true);
        
        // Enable docked camera
        if (dockedCamera != null)
        {
            dockedCamera.Priority = originalCameraPriority + dockedCameraPriorityBoost;
        }
        
        // Switch to Dock input mode
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToDockMode();
        }
        
        onDocked?.Invoke();
        Debug.Log($"Docked at {gameObject.name}");
    }

    /// <summary>
    /// Exits the docked state. Call this from your UI button or other trigger.
    /// </summary>
    public void Undock()
    {
        if (!isDocked) return;
        
        isDocked = false;
        
        // Hide docked UI
        SetDockedCanvasChildrenActive(false);
        
        // Restore camera priority
        if (dockedCamera != null)
        {
            dockedCamera.Priority = originalCameraPriority;
        }
        
        // Switch back to Player input map
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToPlayerMap();
        }
        
        // Show prompt again if player is still in range
        if (playerInRange)
        {
            SetPromptCanvasVisible(true);
        }
        
        onUndocked?.Invoke();
        Debug.Log($"Undocked from {gameObject.name}");
    }

    /// <summary>
    /// Forces undock without triggering input map switch (for cleanup scenarios).
    /// </summary>
    private void ForceUndock()
    {
        isDocked = false;
        SetDockedCanvasChildrenActive(false);
        
        if (dockedCamera != null)
        {
            dockedCamera.Priority = originalCameraPriority;
        }
    }

    /// <summary>
    /// Handles when Dock mode is closed externally (e.g., via Cancel/Escape).
    /// </summary>
    private void HandleDockModeClosed()
    {
        if (isDocked)
        {
            // Dock mode was closed externally, clean up without re-triggering input switch
            isDocked = false;
            SetDockedCanvasChildrenActive(false);
            
            if (dockedCamera != null)
            {
                dockedCamera.Priority = originalCameraPriority;
            }
            
            if (playerInRange)
            {
                SetPromptCanvasVisible(true);
            }
            
            onUndocked?.Invoke();
        }
    }

    private void SetPromptCanvasVisible(bool visible)
    {
        if (promptCanvas != null)
        {
            promptCanvas.gameObject.SetActive(visible);
        }
    }

    private void SetDockedCanvasChildrenActive(bool active)
    {
        if (dockedCanvas == null) return;
        
        foreach (Transform child in dockedCanvas.transform)
        {
            child.gameObject.SetActive(active);
        }
    }
}
