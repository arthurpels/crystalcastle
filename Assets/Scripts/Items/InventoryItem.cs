using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem {
    public ItemData itemData;
    public bool isEquiped {get; private set; } = false;
    public ItemSlot itemSlot  {get; private set; } = null;

    public InventoryItem(ItemData itemData) {
        this.itemData = itemData;
    }

    public void Equip(ItemSlot itemSlot) {
        this.itemSlot = itemSlot;
        isEquiped = true;
    }

    public void Unequip() {
        itemSlot = null;
        isEquiped = false;
    }


}
