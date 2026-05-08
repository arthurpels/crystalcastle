using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour {
    public static PlayerInventory Instance { get; private set; }

    [Header("Slots")]
    public ItemSlot leftHandSlot;
    public HandItem flashlight;
    public ItemSlot rightHandSlot; // Левая рука — отдельно, под фонарик

    [Header("Settings")]
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public LayerMask interactableLayer;

    [Header("Inventory")]
    public List<ItemData> inventory = new();
    public int maxSlots = 6;

    // События для UI
    public static event Action OnInventoryChanged;

    private Camera _cam;
    private WorldItem _targetPickup;

    private void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _cam = Camera.main;
    }

    private void Update() {
        ScanForPickup();
        rightHandSlot.CurrentItem?.OnTick(Time.deltaTime);
    }

    private void ScanForPickup() {
        _targetPickup = null;
        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, 3f, interactableLayer)) {
            if (hit.collider.TryGetComponent(out WorldItem wi) && wi.data != null)
                _targetPickup = wi;
        }
    }

    public void TryPickup() {
        if (_targetPickup != null && inventory.Count < maxSlots) {
            Add(_targetPickup.data);
            _targetPickup.Pickup();
        }
    }

    public void UseRightHandItem() {
        rightHandSlot.CurrentItem?.OnUse();
    }

    public void Add(ItemData data) {
        if (data == null) return;
        inventory.Add(data);
        OnInventoryChanged?.Invoke();

        if (rightHandSlot.CurrentItem == null)
            Equip(data);
    }

    public void Equip(ItemData data) {
        if (data == null || !inventory.Contains(data)) return;
        if (rightHandSlot.CurrentItem != null) Unequip();

        var handObj = Instantiate(data.handPrefab, rightHandSlot.holdPoint);
        handObj.transform.localPosition = Vector3.zero;
        handObj.transform.localRotation = Quaternion.identity;

        var handItem = handObj.GetComponent<HandItem>();
        if (handItem != null) {
            handItem.data = data;
            handItem.OnEquip();
            rightHandSlot.CurrentItem = handItem;
        }
    }

    public void Unequip() {
        if (rightHandSlot.CurrentItem == null) return;
        rightHandSlot.CurrentItem.OnUnequip();
        Destroy(rightHandSlot.CurrentItem.gameObject);
        rightHandSlot.CurrentItem = null;
    }

    public void Drop() {
        var item = rightHandSlot.CurrentItem;
        if (item == null || item.data == null) return;

        Unequip();
        inventory.Remove(item.data);
        OnInventoryChanged?.Invoke();

        // Спавн в мире
        Vector3 dropPos = _cam.transform.position + _cam.transform.forward * 0.7f + Vector3.down * 0.3f;
        var dropped = Instantiate(item.data.worldPrefab, dropPos, Quaternion.identity);

        if (dropped.TryGetComponent(out Rigidbody rb)) {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(_cam.transform.forward * 3f, ForceMode.Impulse);
        }
    }

    public void RemoveFromInventory(ItemData data) {
        if (rightHandSlot.CurrentItem?.data == data) Unequip();
        inventory.Remove(data);
        OnInventoryChanged?.Invoke();
    }
}