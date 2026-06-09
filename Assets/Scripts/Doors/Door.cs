using UnityEngine;

public abstract class Door : MonoBehaviour, IInteractable, ISaveable {
    [Header("State")]
    [SerializeField] protected bool isOpen;
    [SerializeField] protected bool isLocked;

    [Header("Config")]
    [SerializeField] protected DoorConfig config;

    public event System.Action<bool> OnDoorStateChanged;

    // Выставляется DoorPowerAdapter: true = питания нет, дверь заблокирована
    public bool IsPowerLocked { get; set; }

    // === IInteractable ===
    public virtual string PromptText {
        get {
            if (IsPowerLocked) return "Нет питания";
            if (!isOpen && isLocked)
                return config.requiresKey ? "Нужен ключ" : "Открыть";
            return isOpen ? "Закрыть" : "Открыть";
        }
    }

    public void Interact() {
        if (!CanOpen()) return;
        Toggle();
        OnStateChanged();
        OnDoorStateChanged?.Invoke(isOpen);
    }

    protected virtual bool CanOpen() {
        if (IsPowerLocked) {
            PlaySound(config?.lockedSound);
            return false;
        }
        if (isLocked && config?.requiresKey == true) {
            var inventory = FindObjectOfType<PlayerInventory>();
            if (inventory == null || !inventory.HasItem(config.keyItem)) {
                PlaySound(config?.lockedSound);
                return false;
            }
            isLocked = false;
        }
        return true;
    }

    protected void Toggle() {
        isOpen = !isOpen;
        PlaySound(isOpen ? config?.openSound : config?.closeSound);
        if (isOpen && config?.autoClose == true)
            Invoke(nameof(ForceClose), config.closeDelay);
    }

    public void ForceClose() {
        if (!isOpen) return;
        isOpen = false;
        OnStateChanged();
        OnDoorStateChanged?.Invoke(false);
        PlaySound(config?.closeSound);
    }

    protected abstract void OnStateChanged();

    protected void PlaySound(AudioClip clip) {
        if (TryGetComponent(out AudioSource audio) && clip != null)
            audio.PlayOneShot(clip);
    }

    public void Unlock() => isLocked = false;
    public void Lock()   => isLocked = true;
    public bool IsOpen   => isOpen;
    public bool IsLocked => isLocked;
    public DoorConfig DoorConfig => config;

    // ── ISaveable ────────────────────────────────────────────────────────

    [System.Serializable]
    private struct DoorSaveData { public bool isOpen; public bool isLocked; }

    public string CaptureState() =>
        JsonUtility.ToJson(new DoorSaveData { isOpen = this.isOpen, isLocked = this.isLocked });

    public void RestoreState(string json)
    {
        var d    = JsonUtility.FromJson<DoorSaveData>(json);
        isOpen   = d.isOpen;
        isLocked = d.isLocked;
        OnStateChangedImmediate();  // мгновенный снэп, без анимации
    }

    /// <summary>Применить состояние мгновенно (без анимации). Переопределяй в подклассах.</summary>
    protected virtual void OnStateChangedImmediate() => OnStateChanged();
}