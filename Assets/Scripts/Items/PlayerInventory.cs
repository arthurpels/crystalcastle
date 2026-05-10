using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour {
    public static PlayerInventory Instance { get; private set; }

    [Header("Slots")]
    public ItemSlot leftHandSlot;
    public ItemData flashlight;
    public ItemSlot rightHandSlot;

    [Header("Settings")]
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

        Add(flashlight);
        Equip(flashlight, leftHandSlot);
        
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
        // 1. Проверка: есть ли таргет и место в инвентаре?
        if (_targetPickup == null || _targetPickup.data == null) return;
        if (inventory.Count >= maxSlots) return; // или логика стаков

        // 2. Добавляем данные в инвентарь
        Add(_targetPickup.data);

        // 3. Говорим предмету в мире: "Забираю" — он просто уничтожается
        _targetPickup.Pickup();

        // 4. Если правая рука пуста — сразу экипируем (опционально)
        if (rightHandSlot.CurrentItem == null)
            Equip(_targetPickup.data, rightHandSlot);
    }
    public void UseRightHandItem() {
        rightHandSlot.CurrentItem?.OnUse();
    }

    public void UseLeftHandItem() {
        leftHandSlot.CurrentItem?.OnUse();
    }

    public void Add(ItemData data) {
        if (data == null) return;
        inventory.Add(data);
        OnInventoryChanged?.Invoke();
    }

    public void Equip(ItemData data, ItemSlot slot) {
        if (data == null || !inventory.Contains(data)) return;
        if (slot.CurrentItem != null) Unequip(slot);

        var handObj = Instantiate(data.handPrefab, slot.holdPoint);
        handObj.transform.localPosition = Vector3.zero;
        handObj.transform.localRotation = Quaternion.identity;

        var handItem = handObj.GetComponent<HandItem>();
        if (handItem != null) {
            handItem.data = data;
            handItem.OnEquip();
            slot.CurrentItem = handItem;
        }
    }

    public void Unequip(ItemSlot slot) {
        if (slot.CurrentItem == null) return;
        slot.CurrentItem.OnUnequip();
        Destroy(slot.CurrentItem.gameObject);
        slot.CurrentItem = null;
    }

    public void Drop(ItemSlot slot) {
        var item = slot.CurrentItem;
        if (item == null || item.data == null) return;

        Unequip(slot);
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
        if (rightHandSlot.CurrentItem?.data == data) Unequip(rightHandSlot);
        if (leftHandSlot.CurrentItem?.data == data) Unequip(leftHandSlot);
        inventory.Remove(data);
        OnInventoryChanged?.Invoke();
    }
}