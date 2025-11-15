using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [SerializeField] private InventorySlot[] slots;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }
    
    public bool AddItem(InventoryItem item, int amount = 1)
    {
        int remaining = amount;
        
        // First, try to add to existing stacks of the same item
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item && !slot.IsFull())
            {
                if (slot.AddItem(item, remaining))
                {
                    return true; // All items added
                }
                else
                {
                    // Partial add, calculate remaining
                    int added = Mathf.Min(remaining, 9 - slot.GetQuantity());
                    remaining -= added;
                }
            }
        }
        
        // Then, try to add to empty slots
        while (remaining > 0)
        {
            bool foundSlot = false;
            
            foreach (var slot in slots)
            {
                if (slot.IsEmpty())
                {
                    int addAmount = Mathf.Min(remaining, 9);
                    slot.SetItem(item, addAmount);
                    remaining -= addAmount;
                    foundSlot = true;
                    break;
                }
            }
            
            if (!foundSlot)
            {
                Debug.Log("Inventory is full! Could not add all items.");
                return false;
            }
        }
        
        return true;
    }
    
    public bool RemoveItem(InventoryItem item, int amount = 1)
    {
        int remaining = amount;
        
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                int slotQty = slot.GetQuantity();
                
                if (slotQty >= remaining)
                {
                    slot.RemoveItem(remaining);
                    return true;
                }
                else
                {
                    remaining -= slotQty;
                    slot.RemoveItem(slotQty);
                }
            }
        }
        
        return remaining == 0;
    }
    
    public bool HasItem(InventoryItem item, int amount = 1)
    {
        int count = 0;
        
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                count += slot.GetQuantity();
                
                if (count >= amount)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    public int GetItemCount(InventoryItem item)
    {
        int count = 0;
        
        foreach (var slot in slots)
        {
            if (slot.GetItem() == item)
            {
                count += slot.GetQuantity();
            }
        }
        
        return count;
    }
    
    public void ClearInventory()
    {
        foreach (var slot in slots)
        {
            slot.SetItem(null);
        }
    }
}