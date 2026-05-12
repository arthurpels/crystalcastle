using UnityEngine;
using System;

[CreateAssetMenu(menuName = "Game/Doors/Door Config")]
public class DoorConfig : ScriptableObject {
    public bool autoClose = false;    // Закрывать ли автоматически
    public float closeDelay = 3f;     // Задержка перед авто-закрытием

    [Header("Requirements")]
    public bool requiresKey = false;  // Нужен ли ключ
    public ItemData keyItem;          // Какой предмет является ключом

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
}