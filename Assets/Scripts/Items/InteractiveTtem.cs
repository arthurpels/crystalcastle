using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveTtem : BaseItem {
    [Header("Behaviour")]
    public bool isConsumable;
    public AudioClip useSound;
    public int usageCount = 1;

    private AudioSource audioSource;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
        if (visualModel) visualModel.SetActive(false);
    }

    public override void OnEquip(ItemSlot slot) {

    }

    public override void OnUnequip(ItemSlot slot) { }

    public override void OnUse(ItemSlot slot) {
        if (usageCount <= 0) return;

        // Логика использования
        if (useSound && audioSource) audioSource.PlayOneShot(useSound);
        Debug.Log($"[Item] Использован: {displayName}");

        if (isConsumable) {
            usageCount--;
            if (usageCount <= 0)
                slot.Unequip(); // Автоматически убирает из руки
        }
    }

    public override void OnTick(ItemSlot slot, float dt) { }
}
