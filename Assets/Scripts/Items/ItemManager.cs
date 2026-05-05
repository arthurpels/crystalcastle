using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemManager : MonoBehaviour
{
    [Header("Slots")]
    public ItemSlot leftItemSlot;  // Привязать к LeftHand
    // public ItemSlot rightItemSlot; // Привязать к RightHand

    [Header("StarterItems")]
    public FlashlightItem flashlightPrefab;
    public InteractiveTtem defaultRightItem; // Например, пустой слот или факел
    private void Start()
    {
        // Экипируем стартовые предметы
        if (leftItemSlot && flashlightPrefab)
            leftItemSlot.Equip(flashlightPrefab);
            
        // if (rightItemSlot && defaultRightItem)
        //     rightItemSlot.Equip(defaultRightItem);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        leftItemSlot.Tick(dt);
        // rightItemSlot.Tick(dt);
    }

    public void OnLeftHandAction() => leftItemSlot.Use();
    // public void OnRightHandAction() => rightItemSlot.Use();

    // Методы для вызова из системы подбора предметов в мире
    // public void PickupItem(BaseItem item)
    // {
    //     var slot = item is FlashlightItem ? leftItemSlot : rightItemSlot;
    //     if (slot.CurrentItem != null) slot.Unequip(); // Выбрасываем старый
    //     slot.Equip(item);
    // }
}