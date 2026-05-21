using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable, ISaveable
{
    public ItemData data;

    private void Reset()
    {
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = false;
            col.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }
    }

    public void Pickup()
    {
        // Уведомляем SaveManager перед уничтожением
        SaveManager.Instance?.RegisterDestroyed(this);
        Destroy(gameObject);
    }

    public string PromptText => "Поднять";
    public void Interact() => FindObjectOfType<PlayerInventory>().PickupItem(this);

    // ── ISaveable ──────────────────────────────────────────────────────────
    // WorldItem не имеет изменяемого состояния (только "есть / уничтожен").
    // Факт уничтожения фиксируется в SaveManager.destroyedIds через RegisterDestroyed.
    public string CaptureState()    => "{}";
    public void   RestoreState(string _) { /* ничего — объект либо есть, либо нет */ }
}