using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton manager that handles entering/exiting the harvest interaction state.
/// Switches to dock input mode and enables a screen-space canvas for the harvest UI.
/// 
/// SETUP:
/// 1. Attach to a GameObject with the harvest canvas as a child.
/// 2. Assign the InputMapSwitcher reference (or it will auto-find).
/// 3. Assign the harvest canvas (screen-space) - shown when interaction is active.
/// </summary>
public class HarvestManager : MonoBehaviour
{
    public static HarvestManager Instance { get; private set; }
    
    [Header("Input")]
    [Tooltip("Reference to the InputMapSwitcher. Will auto-find if not set.")]
    [SerializeField] private InputMapSwitcher inputMapSwitcher;
    
    [Header("UI")]
    [Tooltip("Screen-space canvas shown during harvest interaction")]
    [SerializeField] private Canvas harvestCanvas;
    
    [Header("Events")]
    public UnityEvent onEnterInteraction;
    public UnityEvent onExitInteraction;
    
    // State
    private bool isInteracting;
    
    /// <summary>
    /// Returns true if currently in harvest interaction mode.
    /// </summary>
    public bool IsInteracting => isInteracting;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"Multiple HarvestManager instances detected. Destroying duplicate on {gameObject.name}");
            Destroy(this);
            return;
        }
        
        Instance = this;
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
            Debug.LogError($"{gameObject.name}: InputMapSwitcher not found. Harvest interaction will not work.", this);
        }
        
        // Initialize canvas to hidden state
        SetHarvestCanvasActive(false);
    }

    private void OnEnable()
    {
        // Subscribe to Dock mode exit to handle forced exit (e.g., Cancel pressed)
        InputMapSwitcher.OnExitUIMode += HandleUIModeClosed;
    }

    private void OnDisable()
    {
        InputMapSwitcher.OnExitUIMode -= HandleUIModeClosed;
        
        // Clean up state if disabled while interacting
        if (isInteracting)
        {
            ForceExitInteraction();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Enters the harvest interaction state.
    /// </summary>
    public void EnterInteraction()
    {
        if (isInteracting) return;
        
        isInteracting = true;
        
        // Show harvest UI
        SetHarvestCanvasActive(true);
        
        // Switch to UI input mode
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToUIMap();
        }
        
        onEnterInteraction?.Invoke();
        Debug.Log("Entered harvest interaction");
    }

    /// <summary>
    /// Exits the harvest interaction state.
    /// </summary>
    public void ExitInteraction()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        
        // Hide harvest UI
        SetHarvestCanvasActive(false);
        
        // Switch back to Player input map
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToPlayerMap();
        }
        
        onExitInteraction?.Invoke();
        Debug.Log("Exited harvest interaction");
    }

    /// <summary>
    /// Forces exit without triggering input map switch (for cleanup scenarios).
    /// </summary>
    private void ForceExitInteraction()
    {
        isInteracting = false;
        SetHarvestCanvasActive(false);
    }

    /// <summary>
    /// Handles when Dock mode is closed externally (e.g., via Cancel/Escape).
    /// </summary>
    private void HandleUIModeClosed()
    {
        if (isInteracting)
        {
            // Dock mode was closed externally, clean up without re-triggering input switch
            isInteracting = false;
            SetHarvestCanvasActive(false);
            onExitInteraction?.Invoke();
        }
    }

    private void SetHarvestCanvasActive(bool active)
    {
        if (harvestCanvas != null)
        {
            harvestCanvas.gameObject.SetActive(active);
        }
    }
}