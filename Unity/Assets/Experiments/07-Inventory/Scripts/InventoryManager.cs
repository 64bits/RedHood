using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private InventorySlot[] slots;
    [SerializeField] private RectTransform floatingIconTransform; // The UI element following mouse
    [SerializeField] private Image floatingIconImage;           // The Image component of the floating icon
    [SerializeField] private float snapDistance = 50f;           // Distance to trigger snapping

    [Header("Current State")]
    private InventoryItem heldItem;
    private int heldAmount;
    private bool isHoldingItem = false;
    private InventorySlot snappedSlot; // The slot we are currently hovering over

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        // Hide floating icon on start
        floatingIconImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isHoldingItem) return;

        UpdateFloatingIcon();
        CheckForSnapping();

        if (Input.GetMouseButtonDown(0))
        {
            HandlePlacement();
        }
    }

    // This replaces your old AddItem logic
    public bool AddItem(InventoryItem item, int amount = 1)
    {
        // If already holding something, we could either block this or swap (let's block for simplicity)
        if (isHoldingItem) return false;

        heldItem = item;
        heldAmount = amount;
        isHoldingItem = true;

        // Setup the visual "cursor"
        floatingIconImage.sprite = item.icon; // Assuming your InventoryItem has an 'icon' sprite
        floatingIconImage.gameObject.SetActive(true);
        
        return true;
    }

    private void UpdateFloatingIcon()
    {
        // If snapped to a slot, lock the icon to that slot's position
        if (snappedSlot != null)
        {
            floatingIconTransform.position = snappedSlot.transform.position;
        }
        else
        {
            // Otherwise, follow the mouse exactly
            floatingIconTransform.position = Input.mousePosition;
        }
    }

    private void CheckForSnapping()
    {
        snappedSlot = null;
        float closestDist = snapDistance;

        foreach (var slot in slots)
        {
            float dist = Vector2.Distance(Input.mousePosition, slot.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                snappedSlot = slot;
            }
        }
    }

    private void HandlePlacement()
    {
        if (snappedSlot != null)
        {
            // Try to place in the snapped slot
            if (snappedSlot.IsEmpty() || (snappedSlot.GetItem() == heldItem && !snappedSlot.IsFull()))
            {
                snappedSlot.AddItem(heldItem, heldAmount);
                ClearHeldItem();
            }
            // If slot is occupied/full, we do nothing (keep holding it)
        }
        else
        {
            // Clicked outside any slot - Item is lost!
            Debug.Log($"{heldItem.name} dropped and lost.");
            ClearHeldItem();
        }
    }

    private void ClearHeldItem()
    {
        heldItem = null;
        heldAmount = 0;
        isHoldingItem = false;
        floatingIconImage.gameObject.SetActive(false);
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