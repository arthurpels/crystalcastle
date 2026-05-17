using System.Collections.Generic;
using UnityEngine;

public class PowerNetwork : MonoBehaviour {
  public static PowerNetwork Instance { get; private set; }

  [SerializeField]
  private List<PowerGenerator> generators = new();
  [SerializeField]
  private List<PowerNode> _allNodes = new();

  void Awake() {
    if (Instance == null)
      Instance = this;
    else
      Destroy(gameObject);
    CollectNodes();
  }

  void CollectNodes() {
    _allNodes.Clear();
    _allNodes.AddRange(FindObjectsOfType<PowerNode>());
  }

  [ContextMenu("Evaluate Grid")]
  public void Evaluate() {
    HashSet < PowerNode > powered = new();
    Queue < PowerNode > queue = new();

    foreach (var gen in generators) {
      if (!gen.IsActive) continue;
      if (gen.OutputNode != null)
        queue.Enqueue(gen.OutputNode);
    }

    while (queue.Count > 0) {
      var current = queue.Dequeue();

      // 🔥 ФИКС: если на ноде есть выключенный PowerBreaker — контакты разомкнуты
      // Питание не доходит ни до этой ноды, ни дальше по цепи
      var breaker = current.GetComponent < PowerBreaker > ();
      if (breaker != null && !breaker.IsOn) continue;

      if (!powered.Add(current)) continue;

      foreach (var cable in current.connections) {
        if (cable == null || cable.isBroken) continue;
        var neighbor = cable.GetOtherEnd(current);
        if (neighbor != null) queue.Enqueue(neighbor);
      }
    }

    foreach (var node in _allNodes)
      node.SetPowered(powered.Contains(node), true);
  }

  public void RegisterGenerator(PowerGenerator gen) {
    if (!generators.Contains(gen))
      generators.Add(gen);
  }

  public void UnregisterGenerator(PowerGenerator gen) => generators.Remove(gen);
}
