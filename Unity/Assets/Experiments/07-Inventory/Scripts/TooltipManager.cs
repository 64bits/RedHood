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
    [SerializeField] private float offsetY = 10f; // Offset above the slot
    
    private RectTransform currentSlotRect;
    
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
        if (tooltipPanel.activeSelf && currentSlotRect != null)
        {
            // Position tooltip above the slot
            Vector3 slotPos = currentSlotRect.position;
            
            // Calculate position above the slot
            float tooltipHeight = tooltipRect.rect.height;
            float slotHeight = currentSlotRect.rect.height;
            
            Vector2 pos = slotPos;
            pos.y = slotPos.y + (slotHeight / 2f) + tooltipHeight + offsetY;
            
            // Keep tooltip within screen bounds horizontally
            float halfWidth = tooltipRect.rect.width / 2f;
            pos.x = Mathf.Clamp(pos.x, halfWidth, Screen.width - halfWidth);
            
            // If tooltip would go above screen, show it below the slot instead
            if (pos.y > Screen.height)
            {
                pos.y = slotPos.y - (slotHeight / 2f) - offsetY;
                tooltipRect.pivot = new Vector2(0.5f, 1f);
            }
            else
            {
                tooltipRect.pivot = new Vector2(0.5f, 0f);
            }
            
            tooltipRect.position = pos;
        }
    }
    
    public void ShowTooltip(string itemName, string description, RectTransform slotRect)
    {
        nameText.text = itemName;
        descriptionText.text = description;
        currentSlotRect = slotRect;
        tooltipPanel.SetActive(true);
    }
    
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        currentSlotRect = null;
    }
}