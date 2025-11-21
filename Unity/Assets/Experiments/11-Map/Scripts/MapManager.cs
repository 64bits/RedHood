using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private GameObject mapPanel;
    
    [Header("Player Reference")]
    [SerializeField] private Transform playerTransform;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    private void OnEnable()
    {
        InputMapSwitcher.OnEnterMapMode += OpenMap;
        InputMapSwitcher.OnExitMapMode += CloseMap;
    }

    private void OnDisable()
    {
        InputMapSwitcher.OnEnterMapMode -= OpenMap;
        InputMapSwitcher.OnExitMapMode -= CloseMap;
    }

    private void Start()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
        }
    }

    private void OpenMap()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(true);
        }
        
        UpdatePlayerMapPosition();
        Debug.Log("MapManager: Map opened.");
    }

    private void CloseMap()
    {
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
        }
        
        Debug.Log("MapManager: Map closed.");
    }

    private void UpdatePlayerMapPosition()
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("MapManager: Player transform not assigned. Cannot set _PlayerMapPos.", this);
            return;
        }
        
        float multiplier = 1.893f;
        Shader.SetGlobalVector("_PlayerMapPos", new Vector2(playerTransform.position.x * multiplier, playerTransform.position.z * multiplier));
    }
}