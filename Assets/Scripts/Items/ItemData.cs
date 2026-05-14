using UnityEngine;

public enum HandSlot { Any, Left, Right }

[CreateAssetMenu(menuName = "Game/Item Data")]
public class ItemData : ScriptableObject {
    [Header("Meta")]
    public string displayName;
    public Sprite icon;

    [Header("Prefabs")]
    public GameObject handPrefab;  // Модель для руки (без Rigidbody, оптимизирована)
    public GameObject worldPrefab; // Модель для мира (с LOD, Rigidbody, коллайдером)


    [Header("Equipment")]
    public HandSlot allowedHand = HandSlot.Any; // По умолчанию — в любую руку

    [Tooltip("Если true, предмет имеет анимацию только для правой руки")]
    public bool hasRightHandAnimation = false;
}