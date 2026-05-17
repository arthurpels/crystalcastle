using UnityEngine;

[RequireComponent(typeof(Door))]
public class DoorPowerAdapter : MonoBehaviour, IPowerable
{
    public bool IsPowered { get; private set; }

    private Door _door;
    private bool _wasLockedBeforePowerLoss;

    void Awake() => _door = GetComponent<Door>();

    public void OnPowerChanged(bool powered)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        if (!powered)
        {
            _wasLockedBeforePowerLoss = _door.IsLocked;
            _door.Lock();
        }
        else
        {
            // Восстанавливаем только если дверь НЕ требовала ключа изначально,
            // или если она уже была разблокирована до пропажи питания.
            if (!_wasLockedBeforePowerLoss)
                _door.Unlock();
        }
    }
}