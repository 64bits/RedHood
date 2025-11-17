using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Slot Setup")]
    [Tooltip("Optional item to spawn in this slot on start.")]
    [SerializeField] private InventoryItem startingItem = null;
    [Tooltip("The quantity for the starting item. Will be clamped between 1 and MAX_STACK.")]
    [SerializeField] [Min(1)] private int startingQuantity = 1; 

    private InventoryItem currentItem;
    private int quantity = 0;
    private const int MAX_STACK = 1;
    
    private void Start()
    {
        if (startingItem != null)
        {
            int effectiveQuantity = Mathf.Clamp(startingQuantity, 1, MAX_STACK);
            SetItem(startingItem, effectiveQuantity);
        }
    }

    public void SetItem(InventoryItem item, int qty = 1)
    {
        currentItem = item;
        quantity = qty;

        if (item != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            UpdateQuantityDisplay();
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            quantity = 0;
            if (quantityText != null) 
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    public bool AddItem(InventoryItem item, int amount = 1)
    {
        if (currentItem == null)
        {
            SetItem(item, amount);
            return true;
        }

        if (currentItem == item && quantity < MAX_STACK)
        {
            int addAmount = Mathf.Min(amount, MAX_STACK - quantity);
            quantity += addAmount;
            UpdateQuantityDisplay();
            return addAmount == amount; 
        }

        return false;
    }

    public bool RemoveItem(int amount = 1)
    {
        if (currentItem == null) return false;

        quantity -= amount;

        if (quantity <= 0)
        {
            SetItem(null, 0);
        }
        else
        {
            UpdateQuantityDisplay();
        }

        return true;
    }

    private void UpdateQuantityDisplay()
    {
        if (quantity > 1)
        {
            quantityText.text = quantity.ToString();
            quantityText.gameObject.SetActive(true);
        }
        else
        {
            quantityText.gameObject.SetActive(false);
        }
    }

    public InventoryItem GetItem()
    {
        return currentItem;
    }

    public int GetQuantity()
    {
        return quantity;
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public bool IsFull()
    {
        return quantity >= MAX_STACK;
    }

    public bool CanAddItem(InventoryItem item)
    {
        return (currentItem == null) || (currentItem == item && quantity < MAX_STACK);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
        {
            // Assuming TooltipManager is a singleton or static class
            TooltipManager.Instance.ShowTooltip(currentItem.itemName, currentItem.description, GetComponent<RectTransform>());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}