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

    private void OnEnable()
    {
        // Subscribe to the static events
        InputMapSwitcher.OnEnterUIMode += SetSlotChildrenActive;
        InputMapSwitcher.OnExitUIMode += SetSlotChildrenInactive;
    }

    private void OnDisable()
    {
        // IMPORTANT: Always unsubscribe when the object is disabled or destroyed
        InputMapSwitcher.OnEnterUIMode -= SetSlotChildrenActive;
        InputMapSwitcher.OnExitUIMode -= SetSlotChildrenInactive;
    }
    
    // --- New Methods to Control Slot Child Visibility ---

    private void SetSlotChildrenActive()
    {
        // Inventory is open (InputMapSwitcher.OnEnterUIMode fired)
        ToggleSlotChildren(true);
        Debug.Log("InventoryManager: Setting all slot children active.");
    }

    private void SetSlotChildrenInactive()
    {
        // Inventory is closed (InputMapSwitcher.OnExitUIMode fired)
        ToggleSlotChildren(false);
        Debug.Log("InventoryManager: Setting all slot children inactive.");
    }

    private void ToggleSlotChildren(bool state)
    {
        // Iterate through all slots and set the active state of their children
        foreach (var slot in slots)
        {
            // You might need to adjust how you get the children based on your InventorySlot setup.
            // Assuming the children are the visual elements you want to hide/show.
            
            // This is the common way to get children of a transform.
            for (int i = 0; i < slot.transform.childCount; i++)
            {
                slot.transform.GetChild(i).gameObject.SetActive(state);
            }
        }
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