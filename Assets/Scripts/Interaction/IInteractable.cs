using UnityEngine;

public interface IInteractable {
  void Interact();
  string PromptText { get; } // Оставляем на будущее для UI
}
