using UnityEngine;
using UnityEngine.Events;

public class LeverInteractable : MonoBehaviour, IInteractable, ISaveable {
    [SerializeField] private bool isOn;
    [SerializeField] private UnityEvent<bool> onToggle;
    [SerializeField] private Animator animator;

    public string PromptText => isOn ? "Выключить" : "Включить";

    void Start() => UpdateVisual();

    public void Interact() {
        isOn = !isOn;
        onToggle?.Invoke(isOn);
        UpdateVisual();
    }

    void UpdateVisual() {
        if (animator != null)
            animator.SetBool("IsOn", isOn);
    }

    // ── ISaveable ──────────────────────────────────────────────────────────

    [System.Serializable]
    private struct LeverSaveData { public bool isOn; }

    public string CaptureState() =>
        JsonUtility.ToJson(new LeverSaveData { isOn = isOn });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<LeverSaveData>(json);
        isOn  = d.isOn;
        onToggle?.Invoke(isOn);
        UpdateVisual();
    }
}