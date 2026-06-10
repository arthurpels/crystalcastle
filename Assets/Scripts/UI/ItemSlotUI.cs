using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour {
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
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
        if (amountText) amountText.text = item.count.ToString();

        if (item.isEquiped) {
            var inventory = FindObjectOfType<PlayerInventory>();
            if (item.itemSlot == inventory.leftHandSlot)
                HighlightActive(equipLeftButton);

            if (item.itemSlot == inventory.rightHandSlot)
                HighlightActive(equipRightButton);
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

    /// <summary>Подсветить кнопку руки, в которую предмет уже экипирован:
    /// яркая янтарная рамка, белый текст, лёгкое увеличение.</summary>
    private void HighlightActive(Button button) {
        if (button == null) return;
        button.interactable = false;

        var colors = button.colors;
        colors.disabledColor = UITheme.Phosphor; // непрозрачный янтарь → рамка горит
        button.colors = colors;

        button.transform.localScale = Vector3.one * 1.08f;

        var txt = button.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.color = Color.white;
    }
}