using System.Collections.Generic;
using UnityEngine;

public class PowerNode : MonoBehaviour {
  [Tooltip("Кабели, подключенные к этой ноде")]
  public List<PowerCable> connections = new();

  public bool IsPowered { get; private set; }

  private IPowerable[] _consumers;

  void Awake() { _consumers = GetComponentsInChildren<IPowerable>(true); }

  public void SetPowered(bool powered, bool notify) {
    if (IsPowered == powered)
      return;
    IsPowered = powered;

    if (notify) {
      foreach (var c in _consumers)
        c.OnPowerChanged(powered);
    }
  }
}
