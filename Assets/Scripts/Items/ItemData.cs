using UnityEngine;

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Meta")]
    public string displayName;
    public Sprite icon;
    public int maxStack = 1;

    [Header("Prefabs")]
    public GameObject handPrefab;  // Модель для руки (без Rigidbody, оптимизирована)
    public GameObject worldPrefab; // Модель для мира (с LOD, Rigidbody, коллайдером)

    [Header("Stats")]
    public float weight = 1f;
    public float pickupRange = 2f;
    // ... damage, durability и т.д.
}