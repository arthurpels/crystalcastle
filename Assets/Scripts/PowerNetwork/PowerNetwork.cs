using System.Collections.Generic;
using UnityEngine;

public class PowerNetwork : MonoBehaviour {
  public static PowerNetwork Instance { get; private set; }

  [SerializeField] private List<PowerGenerator> generators = new();
  [SerializeField]  private List<PowerNode> _allNodes = new();

  void Awake() {
    if (Instance == null) Instance = this;
    else Destroy(gameObject);
    CollectNodes();
    // Evaluate();


  }

  void CollectNodes() {
    _allNodes.Clear();
    _allNodes.AddRange(FindObjectsOfType<PowerNode>());
  }

  [ContextMenu("Evaluate Grid")]
  public void Evaluate() {
    CollectNodes(); // Пересобираем на всякий случай (если объекты спавнились позже)

    HashSet<PowerNode> powered = new();
    Queue<PowerNode> queue = new();

    foreach (var gen in generators) {
      if (gen == null || !gen.IsActive) continue;
      if (gen.OutputNode != null)
        queue.Enqueue(gen.OutputNode);
    }

    while (queue.Count > 0) {
      var current = queue.Dequeue();

      // 🔥 ФИКС: если на ноде выключенный Breaker — контакты разомкнуты, дальше не идём
      var breaker = current.GetComponent<PowerBreaker>();
      if (breaker != null && !breaker.IsOn) continue;

      if (!powered.Add(current)) continue;

      if (current.connections == null) continue;

      foreach (var cable in current.connections) {
        if (cable == null || cable.isBroken) continue;
        var neighbor = cable.GetOtherEnd(current);
        if (neighbor != null) queue.Enqueue(neighbor);
      }
    }

    foreach (var node in _allNodes) {
      if (node == null) continue;
      node.SetPowered(powered.Contains(node), true);
    }
  }

  public void RegisterGenerator(PowerGenerator gen) {
    if (gen != null && !generators.Contains(gen)) generators.Add(gen);
  }

  public void UnregisterGenerator(PowerGenerator gen) => generators.Remove(gen);
}