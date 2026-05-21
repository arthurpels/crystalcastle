using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem {
    public ItemData itemData;
    public bool isEquiped { get; private set; } = false;
    public ItemSlot itemSlot { get; private set; } = null;

    public int count { get; private set; } = 1;  // НОВОЕ: количество в стаке

    public InventoryItem(ItemData itemData, int initialCount = 1) {
        this.itemData = itemData;
        this.count = initialCount;
    }

    public void Equip(ItemSlot itemSlot) {
        this.itemSlot = itemSlot;
        isEquiped = true;
    }

    public void Unequip() {
        itemSlot = null;
        isEquiped = false;
    }

    public bool CanAddToStack(int amount) => itemData.isStackable && count + amount <= itemData.maxStackSize;

    public void AddToStack(int amount) => count += amount;

    public bool RemoveFromStack(int amount) {
        if (count < amount) return false;
        count -= amount;
        return true;
    }


}
