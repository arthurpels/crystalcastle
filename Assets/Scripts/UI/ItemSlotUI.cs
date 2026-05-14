using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour {
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button equipLeftButton;
    [SerializeField] private Button equipRightButton;
    [SerializeField] private Button unequipButton;
    [SerializeField] private Button dropButton;

    private InventoryItem inventoryItem;
    private InventoryUI parentUI;

    public void Setup(InventoryItem item, InventoryUI parent) {
        inventoryItem = item;
        parentUI = parent;

        if (iconImage && item.itemData.icon) iconImage.sprite = item.itemData.icon;
        if (nameText) nameText.text = item.itemData.displayName;

        if (item.isEquiped) {
            var inventory = FindObjectOfType<PlayerInventory>();
            if (item.itemSlot == inventory.leftHandSlot)
                disableButton(equipLeftButton);

            if (item.itemSlot == inventory.rightHandSlot)
                disableButton(equipRightButton);
        } else {
            disableButton(unequipButton);
        }
        switch (item.itemData.allowedHand) {
            case HandSlot.Left:
                disableButton(equipRightButton);
                break;
            case HandSlot.Right:
                disableButton(equipLeftButton);
                break;
        }
        equipRightButton.onClick.AddListener(() => parentUI.EquipItemToRight(inventoryItem));
        equipLeftButton.onClick.AddListener(() => parentUI.EquipItemToLeft(inventoryItem));
        unequipButton.onClick.AddListener(() => parentUI.UnequipItem(inventoryItem));
        dropButton.onClick.AddListener(() => parentUI.DropItem(inventoryItem));
    }

    private void disableButton(Button button) {
        button.interactable = false;

        var colors = button.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        button.colors = colors;
    }
}