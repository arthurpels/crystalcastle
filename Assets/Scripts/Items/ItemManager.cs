using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemManager : MonoBehaviour
{
    [Header("Слоты")]
    public ItemSlot leftItemSlot;  // Привязать к LeftHand
    public ItemSlot rightItemSlot; // Привязать к RightHand

    [Header("Стартовые предметы")]
    public FlashlightItem flashlightPrefab;
    public InteractiveTtem defaultRightItem; // Например, пустой слот или факел

    [Header("Управление")]
    public KeyCode leftHandActionKey = KeyCode.F;
    public KeyCode rightHandActionKey = KeyCode.G;
    private bool _isLeftHandActive = true; // Какой рукой сейчас управляем

    private void Start()
    {
        // Экипируем стартовые предметы
        if (leftItemSlot && flashlightPrefab)
            leftItemSlot.Equip(flashlightPrefab);
            
        if (rightItemSlot && defaultRightItem)
            rightItemSlot.Equip(defaultRightItem);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        leftItemSlot.Tick(dt);
        rightItemSlot.Tick(dt);

        // Действия
        if (Input.GetKeyDown(leftHandActionKey))
            leftItemSlot.Use();
            
        if (Input.GetKeyDown(rightHandActionKey))
            rightItemSlot.Use();
    }

    // Методы для вызова из системы подбора предметов в мире
    public void PickupItem(BaseItem itemPrefab)
    {
        var slot = targetHand == HandType.Left ? leftItemSlot : rightItemSlot;
        if (slot.CurrentItem != null) slot.Unequip(); // Выбрасываем старый
        slot.Equip(itemPrefab);
    }
}