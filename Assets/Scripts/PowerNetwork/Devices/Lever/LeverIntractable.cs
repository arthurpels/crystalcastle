using UnityEngine;
using UnityEngine.Events;

public class LeverInteractable : MonoBehaviour, IInteractable {
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
}