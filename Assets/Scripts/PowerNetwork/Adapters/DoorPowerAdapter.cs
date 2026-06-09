using UnityEngine;

[RequireComponent(typeof(Door))]
public class DoorPowerAdapter : MonoBehaviour, IPowerable
{
    public bool IsPowered { get; private set; }
    public bool IsDoorOpen => _door != null && _door.IsOpen;

    private Door _door;

    void Awake() => _door = GetComponent<Door>();

    public void OnPowerChanged(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        _door.IsPowerLocked = !powered;

        // Питание пропало — дверь закрывается
        if (!powered && _door.IsOpen)
            _door.ForceClose();
    }

    // Вызывается кнопками
    public void TryToggle()
    {
        if (!IsPowered) return;
        _door.Interact();
    }
}
