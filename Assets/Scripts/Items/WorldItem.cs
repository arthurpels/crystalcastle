using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WorldItem : MonoBehaviour, IInteractable, ISaveable
{
    public ItemData data;

    /// <summary>
    /// Количество предметов в этом мировом объекте.
    /// Актуально для стакуемых ItemData (патроны, аптечки и т.д.).
    /// </summary>
    public int amount = 1;

    /// <summary>
    /// true — объект создан во время игры (дроп из инвентаря, из ящика).
    /// false — объект расставлен в сцене в редакторе.
    ///
    /// RuntimeSpawned-предметы сохраняются в SaveFile.spawnedItems (не в objects),
    /// т.к. их SaveableIdentity.Id меняется при каждом пересоздании сцены.
    /// </summary>
    [HideInInspector] public bool isRuntimeSpawned = false;

    private void Reset()
    {
        if (TryGetComponent(out Collider col))
        {
            col.isTrigger = false;
            col.gameObject.layer = LayerMask.NameToLayer("InteractableItems");
        }
    }

    public void Pickup()
    {
        // Регистрируем уничтожение только для scene-объектов со стабильным ID.
        // Runtime-объекты просто исчезают — они не были в destroyedIds.
        if (!isRuntimeSpawned)
        {
            if (SaveManager.Instance != null)
                SaveManager.Instance.RegisterDestroyed(this);
        }
        Destroy(gameObject);
    }

    public string PromptText => data != null ? "Поднять" : null;

    public void Interact()
    {
        var inv = FindObjectOfType<PlayerInventory>();
        if (inv != null)
            inv.PickupItem(this);
    }

    // ── ISaveable ──────────────────────────────────────────────────────────
    // Scene-объекты: сохраняются только факт уничтожения через destroyedIds.
    // Runtime-объекты: сохраняются в SaveFile.spawnedItems (SaveManager).
    // CaptureState/RestoreState здесь не используются по-настоящему,
    // но интерфейс нужен чтобы SaveManager нашёл объект через GetComponent.
    public string CaptureState()    => "{}";
    public void   RestoreState(string _) { }
}
