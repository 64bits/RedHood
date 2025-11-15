using UnityEngine;

// Example script showing how to add items to inventory
public class ItemPickupExample : MonoBehaviour
{
    [SerializeField] private InventoryItem item;
    [SerializeField] private int quantity = 1;
    
    // Alternative: Manual pickup with key press
    public void PickupItem()
    {
        if (InventoryManager.Instance.AddItem(item, quantity))
        {
            Debug.Log($"Picked up {quantity}x {item.itemName}");
            Destroy(gameObject);
        }
    }
}