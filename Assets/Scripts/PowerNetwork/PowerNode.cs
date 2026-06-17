using System.Collections.Generic;
using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public List<PowerCable> connections = new();
    public bool IsPowered { get; private set; }

    /// <summary>
    /// Порядок, в котором BFS питания добрался до ноды (меньше = ближе к источнику).
    /// Используется проводами, чтобы текстура потока бежала в сторону течения тока.
    /// int.MaxValue = нода обесточена.
    /// </summary>
    public int ReachOrder = int.MaxValue;

    private IPowerable[] _consumers;

    void Awake()
    {
        _consumers = GetComponentsInChildren<IPowerable>(true);
    }

    public void SetPowered(bool powered, bool notify, bool force = false)
    {
        if (IsPowered == powered && !force) return;
        IsPowered = powered;

        if (notify && _consumers != null)
        {
            foreach (var c in _consumers)
                if (c != null) c.OnPowerChanged(powered, force);
        }
    }
}