using System.Collections.Generic;
using UnityEngine;

public class PowerNode : MonoBehaviour
{
    public List<PowerCable> connections = new();
    public bool IsPowered { get; private set; }

    private IPowerable[] _consumers;

    void Awake()
    {
        _consumers = GetComponentsInChildren<IPowerable>(true);
    }

    public void SetPowered(bool powered, bool notify)
    {
        if (IsPowered == powered) return;
        IsPowered = powered;

        if (notify && _consumers != null)
        {
            foreach (var c in _consumers)
                if (c != null) c.OnPowerChanged(powered);
        }
    }
}