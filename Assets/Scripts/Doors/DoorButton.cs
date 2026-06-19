using UnityEngine;

public class DoorButton : MonoBehaviour, IInteractable, ISaveable
{
    [SerializeField] private DoorPowerAdapter[] doors;

    [Header("Замок по ключ-карте")]
    [Tooltip("Требовать ключ-карту для использования кнопки.")]
    [SerializeField] private bool requiresKeycard = false;

    [Tooltip("ItemData карты-ключа, которая нужна.")]
    [SerializeField] private ItemData keycard;

    [Tooltip("После первого успешного считывания остаётся разблокированной.")]
    [SerializeField] private bool stayUnlocked = true;

    [Tooltip("Забрать карту из инвентаря при считывании.")]
    [SerializeField] private bool consumeKeycard = false;

    [Header("Звуки (опц.)")]
    [SerializeField] private AudioClip grantedSound;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] [Range(0f, 1f)] private float volume = 0.8f;

    private bool _unlocked;

    public void Interact()
    {
        // Авторизация по карте (если включена и ещё не пройдена).
        if (requiresKeycard && !_unlocked && !TryAuthorize())
            return;

        foreach (var door in doors)
            if (door != null) door.TryToggle();
    }

    private bool TryAuthorize()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null || keycard == null) return false;

        if (!inv.HasItem(keycard))
        {
            HintUI.Instance?.ShowHint($"Требуется: {keycard.displayName}");
            PlaySound(deniedSound);
            return false;
        }

        if (consumeKeycard) inv.Consume(keycard, 1);
        if (stayUnlocked)   _unlocked = true;
        PlaySound(grantedSound);
        return true;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }

    public string PromptText
    {
        get
        {
            if (requiresKeycard && !_unlocked)
            {
                bool has = keycard != null && PlayerInventory.Instance != null
                           && PlayerInventory.Instance.HasItem(keycard);
                return has ? "Считать ключ-карту" : "Заблокировано: нужна ключ-карта";
            }

            if (doors.Length == 0 || doors[0] == null) return "Кнопка";
            if (!doors[0].IsPowered) return "Нет питания";
            return doors[0].IsDoorOpen ? "Закрыть дверь" : "Открыть дверь";
        }
    }

    // ── ISaveable ──────────────────────────────────────────────────────────
    [System.Serializable]
    private struct ButtonSaveData { public bool unlocked; }

    public string CaptureState() =>
        JsonUtility.ToJson(new ButtonSaveData { unlocked = _unlocked });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<ButtonSaveData>(json);
        _unlocked = d.unlocked;
    }
}
