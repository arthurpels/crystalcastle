using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour
{
    public ItemData data; // Ссылка на данные
    
    private void Reset()
    {
        // Авто-настройка коллайдера
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = true;
            col.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
    }
    public void Pickup()
    {
        // 1. Сообщаем инвентарю: "Возьми меня"
        PlayerInventory.Instance?.Add(data);
        
        // 2. Уничтожаем визуал в мире (физика больше не нужна)
        Destroy(gameObject);
    }
}