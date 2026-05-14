using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour {
    public static PlayerInventory Instance { get; private set; }

    [Header("Slots")]
    public ItemSlot leftHandSlot;
    public ItemData flashlight;
    public ItemSlot rightHandSlot;

    [Header("Inventory")]
    public List<InventoryItem> inventory = new();
    public int maxSlots = 6;

    // События для UI
    public event Action OnInventoryChanged;

    private Camera _cam;

    private void Awake() {
        if (Instance == null) Instance = this;
        // else Destroy(gameObject);

        _cam = Camera.main;

        InventoryItem flashlightItem = Add(flashlight);
        Equip(flashlightItem, leftHandSlot);

    }

    private void Update() {

        rightHandSlot.SpawnedItem?.OnTick(Time.deltaTime);
        leftHandSlot.SpawnedItem?.OnTick(Time.deltaTime);
    }

    public void PickupItem(WorldItem worldItem) {

        if (inventory.Count < maxSlots) {
            InventoryItem item = Add(worldItem.data);

            worldItem.Pickup();

            if (rightHandSlot.CurrentItem == null)
                Equip(item, rightHandSlot);
        }
    }
    
    public InventoryItem Add(ItemData data) {
        if (data == null) return null;
        InventoryItem inventoryItem = new(data);
        inventory.Add(inventoryItem);
        OnInventoryChanged?.Invoke();

        return inventoryItem;
    }

    public void Equip(InventoryItem item, ItemSlot slot) {
        if (slot.CurrentItem != null) Unequip(slot);
        if (item.isEquiped) Unequip(item.itemSlot);

        var handObj = Instantiate(item.itemData.handPrefab, slot.holdPoint);
        handObj.transform.localPosition = Vector3.zero;
        handObj.transform.localRotation = Quaternion.identity;

        var handItem = handObj.GetComponent<HandItem>();
        if (handItem != null) {
            handItem.data = item.itemData;
            handItem.OnEquip();
            slot.Equip(item, handItem);
            item.Equip(slot);
        }
    }

    public void Unequip(ItemSlot slot) {
        if (slot.CurrentItem == null) return;
        slot.CurrentItem.Unequip();
        slot.Unequip();
    }

    public void DropFromInventory(InventoryItem item) {
        if (item.isEquiped) {
            Unequip(item.itemSlot);
        }

        // Удаляем из списка
        inventory.Remove(item);
        OnInventoryChanged?.Invoke();

        // Спавним в мире
        Vector3 dropPos = _cam.transform.position + _cam.transform.forward * 0.7f + Vector3.down * 0.3f;
        DropItemToWorld(item.itemData, dropPos);
    }

    private void DropItemToWorld(ItemData data, Vector3 pos) {
        if (data.worldPrefab == null) return;
        var dropped = Instantiate(data.worldPrefab, pos, Quaternion.identity);
        if (dropped.TryGetComponent(out Rigidbody rb)) {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
        }
    }

    public void DropFromSlot(ItemSlot slot) {
        if (slot.CurrentItem == null) return;

        DropFromInventory(slot.CurrentItem);
        // var item = slot.CurrentItem;
        // if (item == null || item.data == null) return;

        // Unequip(slot);

        // inventory.Remove(item.data);
        // OnInventoryChanged?.Invoke();
        
        // Vector3 dropPos = _cam.transform.position + _cam.transform.forward * 0.7f + Vector3.down * 0.3f;
        // DropItemToWorld(item.data, dropPos);
    }

    public void RemoveFromInventory(InventoryItem item) {
        // if (rightHandSlot.CurrentItem == inventoryItem.itemData) Unequip(rightHandSlot);
        // if (leftHandSlot.CurrentItem == inventoryItem.itemData) Unequip(leftHandSlot);
        if (item.isEquiped) {
            Unequip(item.itemSlot);
        }
        inventory.Remove(item);
        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(ItemData data) {
        return inventory.Any(item => item.itemData == data);
    }

    public void UseRightHandItem() {
        rightHandSlot.SpawnedItem?.OnUse();
    }

    public void UseLeftHandItem() {
        leftHandSlot.SpawnedItem?.OnUse();
    }

}
