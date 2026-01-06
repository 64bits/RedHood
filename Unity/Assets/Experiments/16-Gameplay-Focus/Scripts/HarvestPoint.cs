using UnityEngine;

[CreateAssetMenu(fileName = "NewHarvestPoint", menuName = "Harvesting/Harvest Point")]
public class HarvestPoint : ScriptableObject
{
    public string harvestTitle;
    public Sprite harvestIcon;
    
    [Tooltip("The actual item given to the player upon successful harvest.")]
    public InventoryItem itemToDispense; // Assumes InventoryItem is also a ScriptableObject
}