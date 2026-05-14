using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour {
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private CursorController cursorController;

    private List<ItemSlotUI> activeSlots = new();
    private bool isOpen = false;

    private void Awake() {
        if (inventory == null) inventory = FindObjectOfType<PlayerInventory>();
        if (inputHandler == null) inputHandler = FindObjectOfType<PlayerInputHandler>();
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    private void Start() {
        inventory.OnInventoryChanged += Refresh;
        Refresh();
    }

    private void OnDestroy() => inventory.OnInventoryChanged -= Refresh;

    public void Toggle() {
        isOpen = !isOpen;
        inventoryPanel?.SetActive(isOpen);
        if (inputHandler != null) inputHandler.SetInputEnabled(!isOpen);
        if (isOpen) {
            Refresh();
            cursorController.UnlockForUI();
        } else {
            cursorController.LockForGameplay();
        }
    }

    private void Refresh() {
        if (slotContainer == null || slotPrefab == null || inventory == null) return;

        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
        activeSlots.Clear();

        // Создаём заново только актуальные предметы
        foreach (var item in inventory.inventory) {
            var go = Instantiate(slotPrefab, slotContainer);
            var ui = go.GetComponent<ItemSlotUI>();
            if (ui != null) {
                ui.Setup(item, this);
                activeSlots.Add(ui);
            }
        }
    }

    public void EquipItemToRight(InventoryItem item) => inventory.Equip(item, inventory.rightHandSlot);
    public void EquipItemToLeft(InventoryItem item) => inventory.Equip(item, inventory.leftHandSlot);

    public void DropItem(InventoryItem item) => inventory.DropFromInventory(item);
}