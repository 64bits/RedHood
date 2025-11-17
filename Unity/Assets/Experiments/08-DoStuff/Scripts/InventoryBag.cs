using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to a Unity UI Image component.
/// It switches between open and closed sprites based on inventory state.
/// </summary>
[RequireComponent(typeof(Image))]
public class InventoryBag : MonoBehaviour
{
    [Header("Sprite References")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;

    private Image bagImage;

    private void Awake()
    {
        // Get the Image component attached to this GameObject
        bagImage = GetComponent<Image>();
        
        // Start with the closed sprite
        if (closedSprite != null)
        {
            bagImage.sprite = closedSprite;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the static events
        InputMapSwitcher.OnEnterUIMode += ShowOpenBag;
        InputMapSwitcher.OnExitUIMode += ShowClosedBag;
    }

    private void OnDisable()
    {
        // IMPORTANT: Always unsubscribe when the object is disabled or destroyed
        InputMapSwitcher.OnEnterUIMode -= ShowOpenBag;
        InputMapSwitcher.OnExitUIMode -= ShowClosedBag;
    }

    private void ShowOpenBag()
    {
        if (openSprite != null && bagImage != null)
        {
            bagImage.sprite = openSprite;
            Debug.Log("Inventory Bag: Switched to open sprite.");
        }
    }

    private void ShowClosedBag()
    {
        if (closedSprite != null && bagImage != null)
        {
            bagImage.sprite = closedSprite;
            Debug.Log("Inventory Bag: Switched to closed sprite.");
        }
    }
}