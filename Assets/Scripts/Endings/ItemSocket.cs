using UnityEngine;

/// <summary>
/// База для «гнёзд», в которые игрок ставит предмет. Предмет достаточно иметь
/// В ИНВЕНТАРЕ (держать в руке не нужно — на тип гнезда всего один юзабл-итем).
///
/// Постановка списывает 1 шт, спавнит визуал и (опц.) замыкает брейкер стенда —
/// пустое гнездо ток не проводит, поэтому AND-gate сработает только когда все
/// гнёзда заполнены И запитаны.
///
/// IsFilled — единый источник правды «стоит ли предмет». Сохраняется (ISaveable),
/// при загрузке визуал восстанавливается; питание восстанавливает сам брейкер.
///
/// Наследники: TeslaStand (вознесение), BombMount (уничтожение).
/// </summary>
public abstract class ItemSocket : MonoBehaviour, IInteractable, ISaveable
{
    [Header("Socket")]
    [Tooltip("Куда садится визуал поставленного предмета.")]
    [SerializeField] protected Transform socket;

    [Tooltip("Нода стенда в энергосети (для проверки реального питания).")]
    [SerializeField] protected PowerNode node;

    [Tooltip("Брейкер стенда: замыкается при установке, размыкается при изъятии. " +
             "Поставь startsOn = false (пустое гнездо обесточено).")]
    [SerializeField] protected PowerBreaker breaker;

    [Header("Состояние")]
    [Tooltip("Заблокировано (после активации финала — менять нельзя).")]
    [SerializeField] protected bool locked;

    /// <summary>Стоит ли предмет в гнезде. Единый источник правды.</summary>
    public bool IsFilled { get; protected set; }

    /// <summary>Гнездо заполнено И его нода реально под током.</summary>
    public bool IsEnergized => IsFilled && node != null && node.IsPowered;

    /// <summary>Смена состояния (для контроллеров финала / UI).</summary>
    public event System.Action OnChanged;

    // ── Наследники определяют, что принимают и что ставят ────────────────────
    protected abstract ItemData   AcceptedItem { get; }
    protected abstract GameObject PlacedPrefab { get; }

    protected GameObject _placedVisual;

    private string ItemName => AcceptedItem != null ? AcceptedItem.displayName : "предмет";

    // ── IInteractable ────────────────────────────────────────────────────────
    public virtual void Interact()
    {
        if (locked) return;
        if (IsFilled) Retrieve();
        else          TryPlace();
    }

    public virtual string PromptText
    {
        get
        {
            if (locked) return null;
            return IsFilled ? $"Забрать: {ItemName}" : $"Установить: {ItemName}";
        }
    }

    // ── Постановка / изъятие ──────────────────────────────────────────────────
    protected virtual void TryPlace()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || AcceptedItem == null) return;

        if (!inv.HasItem(AcceptedItem))
        {
            HintUI.Instance?.ShowHint($"Нужно: {ItemName}");
            return;
        }

        if (!inv.Consume(AcceptedItem, 1)) return;

        SetVisual(true);
        breaker?.ResetBreaker();   // замыкаем → стенд проводит ток
        OnPlaced();
        OnChanged?.Invoke();
    }

    protected virtual void Retrieve()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null) return;

        // Возвращаем предмет; если места нет — не отдаём.
        if (inv.Add(AcceptedItem, 1) == null)
        {
            HintUI.Instance?.ShowHint("Инвентарь полон");
            return;
        }

        SetVisual(false);
        breaker?.Trip();           // размыкаем → стенд обесточен
        OnRetrieved();
        OnChanged?.Invoke();
    }

    /// <summary>Спавн/удаление визуала + установка флага. Без инвентаря/брейкера.</summary>
    private void SetVisual(bool filled)
    {
        IsFilled = filled;

        if (filled)
        {
            if (_placedVisual == null && PlacedPrefab != null && socket != null)
                _placedVisual = Instantiate(PlacedPrefab, socket.position, socket.rotation, socket);
        }
        else if (_placedVisual != null)
        {
            Destroy(_placedVisual);
            _placedVisual = null;
        }
    }

    /// <summary>Заблокировать (вызывает контроллер финала после активации).</summary>
    public void Lock() => locked = true;

    // Хуки для наследников.
    protected virtual void OnPlaced() { }
    protected virtual void OnRetrieved() { }

    // ── ISaveable ──────────────────────────────────────────────────────────────
    // Сохраняем только факт «стоит/не стоит». Питание восстановит брейкер
    // (он тоже ISaveable), инвентарь — свой сейв. Поэтому тут НЕ трогаем
    // ни брейкер, ни инвентарь — только визуал.
    [System.Serializable]
    private struct SocketSaveData { public bool filled; }

    public string CaptureState() =>
        JsonUtility.ToJson(new SocketSaveData { filled = IsFilled });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<SocketSaveData>(json);
        SetVisual(d.filled);
        OnChanged?.Invoke();
    }
}
