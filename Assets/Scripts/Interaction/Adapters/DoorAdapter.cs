using UnityEngine;

[RequireComponent(typeof(Door))]
public class DoorAdapter : MonoBehaviour, IInteractable {
    private Door _door;

    public float InteractionPriority => 0.5f;

    private void Awake() {
        _door = GetComponent<Door>();
    }

    public string PromptText {
        get {
            if (!_door.IsOpen && _door.IsLocked)
                return _door.DoorConfig.requiresKey ? "Нущен ключ" : "Открыть";
            return _door.IsOpen ? "Закрыть" : "Открыть";
        }
    }

    public void Interact() {
        _door.Interact();
    }
}