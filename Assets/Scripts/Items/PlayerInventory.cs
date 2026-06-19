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
    if (Instance == null)
      Instance = this;
    // else Destroy(gameObject);

    _cam = Camera.main;

    InventoryItem flashlightItem = Add(flashlight);
    Equip(flashlightItem, leftHandSlot);
  }

  private void Update() {

    if (rightHandSlot.SpawnedItem != null) rightHandSlot.SpawnedItem.OnTick(Time.deltaTime);
    if (leftHandSlot.SpawnedItem  != null) leftHandSlot.SpawnedItem.OnTick(Time.deltaTime);
  }

  public void PickupItem(WorldItem worldItem) {
    if (worldItem == null || worldItem.data == null) return;

    // Для стакуемых предметов: Add найдёт существующий стек даже при полном инвентаре.
    // Для нестакуемых: нужен свободный слот.
    InventoryItem item = Add(worldItem.data, worldItem.amount);
    if (item == null) return; // инвентарь полон и стак не подошёл

    worldItem.Pickup();

    if (rightHandSlot.CurrentItem == null)
      Equip(item, rightHandSlot);
  }

  public InventoryItem Add(ItemData data, int amount = 1) {
    if (data == null)
      return null;

    if (data.isStackable) {
      var existing = inventory.Find(item =>
          item.itemData == data && item.CanAddToStack(amount));

      if (existing != null) {
        existing.AddToStack(amount);
        OnInventoryChanged?.Invoke();
        return existing;
      }
    }

    if (inventory.Count >= maxSlots) return null;

    // Для стакуемых предметов создаём стек с нужным количеством (важно при загрузке).
    // Для нестакуемых count всегда = 1.
    InventoryItem inventoryItem = new(data, data.isStackable ? amount : 1);
    inventory.Add(inventoryItem);
    OnInventoryChanged?.Invoke();

    return inventoryItem;
  }

  public void Equip(InventoryItem item, ItemSlot slot) {
    if (item.itemData.allowedHand == HandSlot.Left && slot != leftHandSlot)
      return;
    if (item.itemData.allowedHand == HandSlot.Right && slot != rightHandSlot)
      return;

    if (slot.CurrentItem != null)
      Unequip(slot);
    if (item.isEquiped)
      Unequip(item.itemSlot);

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
    OnInventoryChanged?.Invoke();
  }

  public void Unequip(ItemSlot slot) {
    if (slot.CurrentItem == null)
      return;
    slot.CurrentItem.Unequip();
    slot.Unequip();
    OnInventoryChanged?.Invoke();
  }

  public void DropFromInventory(InventoryItem item) {
    int countToDrop = item.count;
    if (item.isEquiped) Unequip(item.itemSlot);
    inventory.Remove(item);
    OnInventoryChanged?.Invoke();

    Vector3 dropPos = _cam.transform.position + _cam.transform.forward * 0.7f +
                      Vector3.down * 0.3f;
    DropItemToWorld(item.itemData, dropPos, countToDrop);
  }

  /// <summary>
  /// Создаёт WorldItem в мире. Всегда помечает как RuntimeSpawned = true,
  /// чтобы SaveManager сохранял его в spawnedItems (а не destroyedIds).
  /// </summary>
  public void DropItemToWorld(ItemData data, Vector3 pos, int count = 1) {
    if (data == null || data.worldPrefab == null) return;

    var dropped = Instantiate(data.worldPrefab, pos, Quaternion.identity);

    var wi = dropped.GetComponent<WorldItem>();
    if (wi != null) {
      wi.amount           = count;
      wi.isRuntimeSpawned = true;
    }

    if (dropped.TryGetComponent(out Rigidbody rb)) {
      rb.isKinematic = false;
      rb.useGravity  = true;
      rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
    }
  }

  public void DropFromSlot(ItemSlot slot) {
    if (slot.CurrentItem == null)
      return;

    DropFromInventory(slot.CurrentItem);
    // var item = slot.CurrentItem;
    // if (item == null || item.data == null) return;

    // Unequip(slot);

    // inventory.Remove(item.data);
    // OnInventoryChanged?.Invoke();

    // Vector3 dropPos = _cam.transform.position + _cam.transform.forward * 0.7f
    // + Vector3.down * 0.3f; DropItemToWorld(item.data, dropPos);
  }

  public void RemoveFromInventory(InventoryItem item) {
    // if (rightHandSlot.CurrentItem == inventoryItem.itemData)
    // Unequip(rightHandSlot); if (leftHandSlot.CurrentItem ==
    // inventoryItem.itemData) Unequip(leftHandSlot);
    if (item.isEquiped) {
      Unequip(item.itemSlot);
    }
    inventory.Remove(item);
    OnInventoryChanged?.Invoke();
  }

  public bool HasItem(ItemData data, int amount = 1) {
    var item = inventory.Find(i => i.itemData == data);
    return item != null && item.count >= amount;
  }

  /// <summary>ItemData предмета в правой руке (или null). Нужно сокетам (стенды/бомба).</summary>
  public ItemData GetRightHandItem() =>
      rightHandSlot != null ? rightHandSlot.CurrentItem.itemData : null;

  // ── Save system support ──────────────────────────────────────────────────

  /// <summary>
  /// Снять всё экипированное и очистить инвентарь.
  /// Вызывается SaveManager перед восстановлением инвентаря из сохранения.
  /// </summary>
  public void ClearForLoad()
  {
    if (leftHandSlot.CurrentItem  != null) Unequip(leftHandSlot);
    if (rightHandSlot.CurrentItem != null) Unequip(rightHandSlot);
    inventory.Clear();
    OnInventoryChanged?.Invoke();
  }

  public void UseRightHandItem() {
    if (rightHandSlot.SpawnedItem != null) rightHandSlot.SpawnedItem.OnUse();
    OnInventoryChanged?.Invoke();
  }

  public void UseLeftHandItem() {
    if (leftHandSlot.SpawnedItem != null) leftHandSlot.SpawnedItem.OnUse();
    OnInventoryChanged?.Invoke();
  }

  public bool Consume(ItemData data, int amount = 1) {
    var item = inventory.Find(i => i.itemData == data);
    if (item == null) return false;

    if (!item.RemoveFromStack(amount)) return false;

    // Если стак опустел — убираем из инвентаря
    if (item.count <= 0) {
      if (item.isEquiped) Unequip(item.itemSlot);
      inventory.Remove(item);
    }

    OnInventoryChanged?.Invoke();
    return true;
  }
}
