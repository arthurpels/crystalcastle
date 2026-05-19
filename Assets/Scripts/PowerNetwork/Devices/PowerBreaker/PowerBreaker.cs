using UnityEngine;

public class PowerBreaker : MonoBehaviour, IInteractable {
    [Header("Grid")]
    [SerializeField] private PowerNode controlledNode;
    [SerializeField] private bool startsOn = true;

    [Header("Visual")]
    [SerializeField] private Animator animator;
    [SerializeField] private string boolParam = "IsOn";

    public bool IsOn { get; private set; }

    void Start() {
        IsOn = startsOn;
        controlledNode?.SetPowered(IsOn, false);
        PowerNetwork.Instance?.Evaluate(); // Пересчитаем при старте
        UpdateVisual();
    }

    public void Interact() => Toggle();

    public void Trip() {
        if (!IsOn) return;
        IsOn = false;
        controlledNode?.SetPowered(false, true);
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate(); // 🔥
    }

    public void ResetBreaker() {
        if (IsOn) return;
        IsOn = true;
        controlledNode?.SetPowered(true, true);
        UpdateVisual();
        PowerNetwork.Instance?.Evaluate(); // 🔥
    }

    void Toggle() {
        IsOn = !IsOn;
        controlledNode?.SetPowered(IsOn, true);

        PowerNetwork.Instance?.Evaluate(); // 🔥
        UpdateVisual();
    }

    void UpdateVisual() {
        if (animator != null)
            animator.SetBool(boolParam, IsOn);
    }

    public string PromptText => IsOn ? "Обесточить" : "Подать питание";
}