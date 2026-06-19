using System.Collections.Generic;
using UnityEngine;

public class PowerNetwork : MonoBehaviour {
    public static PowerNetwork Instance { get; private set; }

    [SerializeField] private List<PowerGenerator> generators = new();
    [SerializeField] private List<PowerNode>      _allNodes  = new();

    // Все логические элементы (AND-гейты, переключаемые LogicGate, чекеры) —
    // собираются в рантайме, т.к. интерфейс не сериализуется.
    private readonly List<IPowerGate> _gates = new();

    private readonly List<IPowerSource> _runtimeSources = new();

    // Счётчик порядка обхода BFS — для направления потока в проводах.
    private int _reachCounter;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        CollectNodes();
    }

    System.Collections.IEnumerator Start() {
        // Холодный старт: ждём, пока все breaker/generator отработают Start()
        // и сеть устаканится, затем форс-синхронизируем консьюмеров.
        // Иначе обесточенные с самого начала двери/лампы остаются в дефолте
        // (не залочены / выключены не применены), т.к. нода не меняла значение.
        yield return null;
        Evaluate();
        ForceNotifyAll();
    }

    void CollectNodes() {
        _allNodes.Clear();
        _allNodes.AddRange(FindObjectsByType<PowerNode>(FindObjectsSortMode.None));

        _gates.Clear();
        _gates.AddRange(FindObjectsByType<PowerAndGate>(FindObjectsSortMode.None));
        _gates.AddRange(FindObjectsByType<LogicGate>(FindObjectsSortMode.None));
        _gates.AddRange(FindObjectsByType<SignalChecker>(FindObjectsSortMode.None));
        _gates.AddRange(FindObjectsByType<CountdownTimer>(FindObjectsSortMode.None));
    }

    [ContextMenu("Evaluate Grid")]
    public void Evaluate() {
        CollectNodes();

        HashSet<PowerNode> powered = new();

        // Пересчёт ДО СТАБИЛИЗАЦИИ: каждую итерацию строим питание заново от
        // источников + выходов гейтов, удовлетворённых ПРЕДЫДУЩИМ состоянием.
        // Так корректно работают немонотонные элементы (XOR, чекер): добавление
        // сигнала может и ВЫКЛючить выход — инкрементальное накопление это ломало.
        int maxIter = _gates.Count + 2;
        for (int iter = 0; iter <= maxIter; iter++) {
            // Сброс порядка обхода (направление потока) для этого прохода.
            _reachCounter = 0;
            foreach (var node in _allNodes)
                if (node != null) node.ReachOrder = int.MaxValue;

            var next  = new HashSet<PowerNode>();
            var queue = new Queue<PowerNode>();

            // Источники
            foreach (var gen in generators) {
                if (gen == null || !gen.IsActive || gen.OutputNode == null) continue;
                queue.Enqueue(gen.OutputNode);
            }
            foreach (var src in _runtimeSources) {
                if (src == null || !src.IsActive || src.OutputNode == null) continue;
                queue.Enqueue(src.OutputNode);
            }
            // Выходы гейтов, чьё условие выполнено по предыдущему состоянию
            foreach (var gate in _gates) {
                if (gate == null || gate.Output == null) continue;
                if (gate.Compute(powered)) queue.Enqueue(gate.Output);
            }

            RunBFS(queue, next);

            if (next.SetEquals(powered)) { powered = next; break; }
            powered = next;
        }

        // Применяем результат ко всем нодам
        foreach (var node in _allNodes) {
            if (node != null) node.SetPowered(powered.Contains(node), true);
        }
    }

    /// <summary>
    /// Безусловно толкает текущее состояние питания во ВСЕ ноды → консьюмеров.
    /// Вызывать после Evaluate() при загрузке/холодном старте, чтобы пере-синхронизировать
    /// рантайм-состояние консьюмеров (Door.IsPowerLocked, лампы, обогрев), которое
    /// сбрасывается при перезагрузке сцены и не обновляется, если нода не меняла значение.
    /// </summary>
    public void ForceNotifyAll() {
        foreach (var node in _allNodes) {
            if (node != null) node.SetPowered(node.IsPowered, true, force: true);
        }
    }

    // BFS от нескольких стартовых нод, заполняет множество powered
    private void RunBFS(Queue<PowerNode> queue, HashSet<PowerNode> powered) {
        while (queue.Count > 0) {
            var current = queue.Dequeue();

            var breaker = current.GetComponent<PowerBreaker>();
            if (breaker != null && !breaker.IsOn) continue;

            if (!powered.Add(current)) continue;

            // Фиксируем порядок достижения ноды (направление течения тока).
            current.ReachOrder = _reachCounter++;

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
