using System.Collections.Generic;
using UnityEngine;

public class PowerNetwork : MonoBehaviour {
    public static PowerNetwork Instance { get; private set; }

    [SerializeField] private List<PowerGenerator> generators = new();
    [SerializeField] private List<PowerNode>      _allNodes  = new();
    [SerializeField] private List<PowerAndGate>   _andGates  = new();

    private readonly List<IPowerSource> _runtimeSources = new();

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        CollectNodes();
    }

    void CollectNodes() {
        _allNodes.Clear();
        _allNodes.AddRange(FindObjectsByType<PowerNode>(FindObjectsSortMode.None));

        _andGates.Clear();
        _andGates.AddRange(FindObjectsByType<PowerAndGate>(FindObjectsSortMode.None));
    }

    [ContextMenu("Evaluate Grid")]
    public void Evaluate() {
        CollectNodes();

        HashSet<PowerNode> powered = new();
        Queue<PowerNode>   queue   = new();

        // Стартуем BFS от активных генераторов
        foreach (var gen in generators) {
            if (gen == null || !gen.IsActive || gen.OutputNode == null) continue;
            queue.Enqueue(gen.OutputNode);
        }
        foreach (var src in _runtimeSources) {
            if (src == null || !src.IsActive || src.OutputNode == null) continue;
            queue.Enqueue(src.OutputNode);
        }
        RunBFS(queue, powered);

        // AND-гейты: если все входы запитаны → запускаем BFS от выхода.
        // Повторяем пока есть изменения (для цепочек гейтов).
        bool changed = true;
        while (changed) {
            changed = false;
            foreach (var gate in _andGates) {
                if (gate == null || gate.Output == null) continue;
                if (powered.Contains(gate.Output))       continue; // уже запитана

                bool allPowered = true;
                foreach (var input in gate.Inputs) {
                    if (input == null || !powered.Contains(input)) { allPowered = false; break; }
                }

                if (allPowered) {
                    queue.Enqueue(gate.Output);
                    RunBFS(queue, powered);
                    changed = true;
                }
            }
        }

        // Применяем результат ко всем нодам
        foreach (var node in _allNodes) {
            if (node != null) node.SetPowered(powered.Contains(node), true);
        }
    }

    // BFS от нескольких стартовых нод, заполняет множество powered
    private void RunBFS(Queue<PowerNode> queue, HashSet<PowerNode> powered) {
        while (queue.Count > 0) {
            var current = queue.Dequeue();

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
    }

    public void RegisterGenerator(PowerGenerator gen) {
        if (gen != null && !generators.Contains(gen)) generators.Add(gen);
    }

    public void UnregisterGenerator(PowerGenerator gen) => generators.Remove(gen);

    public void RegisterSource(IPowerSource src) {
        if (src != null && !_runtimeSources.Contains(src)) _runtimeSources.Add(src);
    }

    public void UnregisterSource(IPowerSource src) => _runtimeSources.Remove(src);
}
