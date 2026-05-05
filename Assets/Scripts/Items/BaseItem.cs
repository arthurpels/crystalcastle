using UnityEngine;

/// <summary>
/// Базовый класс для любого предмета в руке.
/// Вешается на префаб предмета (вместе с Mesh + Light/Audio).
/// </summary>
public abstract class BaseItem : MonoBehaviour
{
    [Header("Data")]    
    public string displayName = "Item";
    public Sprite icon;
    
    [Header("Visual")]
    [Tooltip("Объект с мешем/светом внутри префаба")]
    public GameObject visualModel;
    
    [Tooltip("Offset")]
    public Vector3 localOffset = Vector3.zero;
    public Vector3 localRotation = Vector3.zero;

    // Вызываются менеджером
    public abstract void OnEquip(ItemSlot slot);
    public abstract void OnUnequip(ItemSlot slot);
    public abstract void OnUse(ItemSlot slot);
    
    // Вызывается каждый кадр, пока предмет в руке
    public virtual void OnTick(ItemSlot slot, float deltaTime) {}
}