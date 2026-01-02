using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // Added for the New Input System

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    
    [Header("Configuration")]
    [SerializeField] private InventorySlot[] slots;
    [SerializeField] private RectTransform floatingIconTransform; 
    [SerializeField] private Image floatingIconImage;           
    [SerializeField] private float snapDistance = 50f;           
    [SerializeField] private Canvas parentCanvas; // Assign your Main Canvas here

    [Header("Current State")]
    private InventoryItem heldItem;
    private int heldAmount;
    private bool isHoldingItem = false;
    private InventorySlot snappedSlot;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        floatingIconImage.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!isHoldingItem) return;

        // NEW INPUT SYSTEM: Reading mouse position
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        UpdateFloatingIcon(mousePosition);
        CheckForSnapping(mousePosition);

        // NEW INPUT SYSTEM: Checking for click
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandlePlacement();
        }
    }

    public bool AddItem(InventoryItem item, int amount = 1)
    {
        if (isHoldingItem) return false;

        heldItem = item;
        heldAmount = amount;
        isHoldingItem = true;

        floatingIconImage.sprite = item.icon;
        floatingIconImage.gameObject.SetActive(true);
        
        return true;
    }

    private void UpdateFloatingIcon(Vector2 mousePosition)
    {
        if (snappedSlot != null)
        {
            // Snap to the slot center
            floatingIconTransform.position = snappedSlot.transform.position;
        }
        else
        {
            // Follow the mouse position
            floatingIconTransform.position = mousePosition;
        }
    }

    private void CheckForSnapping(Vector2 mousePosition)
    {
        snappedSlot = null;
        float closestDist = snapDistance;

        foreach (var slot in slots)
        {
            // Measure distance between mouse and slot in screen space
            float dist = Vector2.Distance(mousePosition, slot.transform.position);
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
            if (snappedSlot.IsEmpty() || (snappedSlot.GetItem() == heldItem && !snappedSlot.IsFull()))
            {
                snappedSlot.AddItem(heldItem, heldAmount);
                ClearHeldItem();
            }
        }
        else
        {
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