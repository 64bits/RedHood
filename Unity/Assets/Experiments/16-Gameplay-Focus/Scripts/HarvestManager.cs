using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Singleton manager that handles entering/exiting the harvest interaction state.
/// Now takes in HarvestPoint data to drive the UI and rewards.
/// </summary>
public class HarvestManager : MonoBehaviour
{
    public static HarvestManager Instance { get; private set; }
    
    [Header("Input")]
    [SerializeField] private InputMapSwitcher inputMapSwitcher;
    
    [Header("UI")]
    [SerializeField] private Canvas harvestCanvas;
    [SerializeField] private TMP_Text harvestTitle;
    [SerializeField] private Image harvestIcon;
    
    [Header("Events")]
    public UnityEvent onEnterInteraction;
    public UnityEvent onExitInteraction;
    
    // State
    private bool isInteracting;
    private HarvestPoint currentHarvestPoint;

    public bool IsInteracting => isInteracting;
    
    /// <summary>
    /// Provides access to the data of the resource currently being harvested.
    /// </summary>
    public HarvestPoint CurrentHarvestPoint => currentHarvestPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (inputMapSwitcher == null)
            inputMapSwitcher = FindFirstObjectByType<InputMapSwitcher>();
        
        SetHarvestCanvasActive(false);
    }

    private void OnEnable() => InputMapSwitcher.OnExitUIMode += HandleUIModeClosed;
    private void OnDisable() => InputMapSwitcher.OnExitUIMode -= HandleUIModeClosed;

    /// <summary>
    /// Enters the harvest interaction state using specific resource data.
    /// </summary>
    /// <param name="point">The ScriptableObject containing title, icon, and item data.</param>
    public void EnterInteraction(HarvestPoint point)
    {
        if (isInteracting || point == null) return;
        
        isInteracting = true;
        currentHarvestPoint = point;

        harvestTitle.text = point.harvestTitle;
        harvestIcon.sprite = point.harvestIcon;
        
        SetHarvestCanvasActive(true);
        
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToUIMap();
        }
        
        onEnterInteraction?.Invoke();
        Debug.Log($"Started harvesting: {point.harvestTitle}");
    }

    public void ExitInteraction()
    {
        if (!isInteracting) return;
        
        isInteracting = false;
        currentHarvestPoint = null; // Clear data on exit
        
        SetHarvestCanvasActive(false);
        
        if (inputMapSwitcher != null)
        {
            inputMapSwitcher.SwitchToPlayerMap();
        }
        
        onExitInteraction?.Invoke();
    }

    private void HandleUIModeClosed()
    {
        if (isInteracting)
        {
            isInteracting = false;
            currentHarvestPoint = null;
            SetHarvestCanvasActive(false);
            onExitInteraction?.Invoke();
        }
    }

    private void SetHarvestCanvasActive(bool active)
    {
        if (harvestCanvas != null) harvestCanvas.gameObject.SetActive(active);
    }
}