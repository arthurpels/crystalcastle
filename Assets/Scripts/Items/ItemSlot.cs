using UnityEngine;

public class ItemSlot : MonoBehaviour
{
    public Transform holdPoint;
    public InventoryItem CurrentItem { get; private set; }
    public HandItem SpawnedItem { get; private set; }

    public void Equip(InventoryItem inventoryItem, HandItem spawnedItem) {
        CurrentItem = inventoryItem;
        SpawnedItem = spawnedItem;
    }

    public void Unequip() {
        CurrentItem = null;
        Destroy(SpawnedItem.gameObject);
        SpawnedItem = null;
    }
}