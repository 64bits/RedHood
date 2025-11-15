using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }
    
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private RectTransform tooltipRect;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        tooltipPanel.SetActive(false);
    }
    
    private void Update()
    {
        if (tooltipPanel.activeSelf)
        {
            // Position tooltip near mouse cursor
            Vector2 pos = Input.mousePosition;
            
            // Offset tooltip to avoid cursor overlap
            pos.x += 15;
            pos.y -= 15;
            
            // Keep tooltip within screen bounds
            float pivotX = pos.x + tooltipRect.rect.width > Screen.width ? 1f : 0f;
            float pivotY = pos.y - tooltipRect.rect.height < 0 ? 0f : 1f;
            
            tooltipRect.pivot = new Vector2(pivotX, pivotY);
            tooltipRect.position = pos;
        }
    }
    
    public void ShowTooltip(string itemName, string description)
    {
        nameText.text = itemName;
        descriptionText.text = description;
        tooltipPanel.SetActive(true);
    }
    
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }
}