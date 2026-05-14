using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Button equipRightButton;
    [SerializeField] private Button equipLeftButton;
    [SerializeField] private Button dropButton;

    private InventoryItem inventoryItem;
    private InventoryUI parentUI;

    public void Setup(InventoryItem item, InventoryUI parent)
    {
        inventoryItem = item;
        parentUI = parent;

        if (iconImage && item.itemData.icon) iconImage.sprite = item.itemData.icon;
        if (nameText) nameText.text = item.itemData.displayName;

        equipRightButton.onClick.AddListener(() => parentUI.EquipItemToRight(inventoryItem));
        equipLeftButton.onClick.AddListener(() => parentUI.EquipItemToLeft(inventoryItem));
        dropButton.onClick.AddListener(() => parentUI.DropItem(inventoryItem));
    }
}