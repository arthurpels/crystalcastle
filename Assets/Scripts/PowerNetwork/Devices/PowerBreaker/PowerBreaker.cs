using UnityEngine;

public class PowerBreaker : MonoBehaviour, IInteractable, ISaveable {
    [Header("Grid")]
    [SerializeField] private PowerNode controlledNode;
    [SerializeField] private bool startsOn = true;

    [Header("Visual")]
    [SerializeField] private Animator animator;
    [SerializeField] private string boolParam = "IsOn";
    [SerializeField] private GameObject enabledGO;
    [SerializeField] private GameObject disabledGO;

    public bool IsOn { get; private set; }

    void Start() {
        IsOn = startsOn;
        controlledNode?.SetPowered(IsOn, false);
        PowerNetwork.Instance?.Evaluate(); // Пересчитаем при старте
        UpdateVisual();
    }

    public void Interact() => Toggle();

    // ВАЖНО: брейкер НЕ питает ноду напрямую. Он лишь меняет IsOn и пересчитывает
    // сеть — Evaluate() сам определит реальное питание ноды и уведомит потребителей.
    // (Раньше тут был оптимистичный SetPowered(..., notify:true) — он уведомлял
    //  потребителей «под током» ещё до пересчёта, из-за чего детонатор/двери
    //  срабатывали на замыкание брейкера, хотя генератор ноду не питал.)

    public void Trip() {
        if (!IsOn) return;
        IsOn = false;
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate();
    }

    public void ResetBreaker() {
        if (IsOn) return;
        IsOn = true;
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate();
    }

    void Toggle() {
        IsOn = !IsOn;
        PowerNetwork.Instance?.Evaluate();
        UpdateVisual();
    }

    void UpdateVisual() {
        if (animator != null)
            animator.SetBool(boolParam, IsOn);
        if (enabledGO  != null) enabledGO.SetActive(IsOn);
        if (disabledGO != null) disabledGO.SetActive(!IsOn);
    }

    public string PromptText => IsOn ? "Обесточить" : "Подать питание";

    // ── ISaveable ──────────────────────────────────────────────────────────

    [System.Serializable]
    private struct BreakerSaveData { public bool isOn; }

    public string CaptureState() =>
        JsonUtility.ToJson(new BreakerSaveData { isOn = IsOn });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<BreakerSaveData>(json);
        IsOn  = d.isOn;
        UpdateVisual();
        // Evaluate() — см. SaveManager.
    }
}