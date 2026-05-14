using UnityEngine;

public abstract class Door : MonoBehaviour {
    [Header("State")]
    [SerializeField] protected bool isOpen;
    [SerializeField] protected bool isLocked;

    [Header("Config")]
    [SerializeField] protected DoorConfig config;

    public event System.Action<bool> OnDoorStateChanged;

    public void Interact() {
        if (!CanOpen()) return;

        Toggle();

        OnStateChanged();

        OnDoorStateChanged?.Invoke(isOpen);
    }

    protected virtual bool CanOpen() {
        if (isLocked && config.requiresKey == true) {
            var inventory = FindObjectOfType<PlayerInventory>();
            if (inventory == null || !inventory.HasItem(config.keyItem)) {
                PlaySound(config?.lockedSound);
                return false;
            }
            isLocked = false;
        }
        return true;
    }

    // === Переключение состояния (общая логика) ===
    protected void Toggle() {
        isOpen = !isOpen;
        PlaySound(isOpen ? config?.openSound : config?.closeSound);

        if (isOpen && config?.autoClose == true)
            Invoke(nameof(Close), config.closeDelay);
    }

    protected void Close() {
        if (isOpen) Toggle();
    }

    protected abstract void OnStateChanged();

    protected void PlaySound(AudioClip clip) {
        if (TryGetComponent(out AudioSource audio) && clip != null)
            audio.PlayOneShot(clip);
    }


    public void Unlock() { isLocked = false; }
    public void Lock() { isLocked = true; }
    public bool IsOpen => isOpen;
    public bool IsLocked => isLocked;

    public DoorConfig DoorConfig => config;
}