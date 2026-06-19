using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// «Замок по последовательности»: ряд входных нод сравнивается с заданным
/// паттерном (вкл/выкл по каждому входу). Если паттерн совпал точно — выходная
/// нода запитывается. Немонотонный элемент: лишний запитанный вход ломает
/// совпадение (PowerNetwork считает сеть до стабилизации, это корректно).
///
/// target[i] = true  → вход i ДОЛЖЕН быть под током
/// target[i] = false → вход i ДОЛЖЕН быть обесточен
/// Длина target должна совпадать с числом входов.
/// </summary>
public class SignalChecker : MonoBehaviour, IPowerGate
{
    [Header("Grid")]
    [SerializeField] private PowerNode[] inputs;
    [SerializeField] private PowerNode   output;

    [Header("Загадка")]
    [Tooltip("Ожидаемый паттерн: галка = вход должен быть запитан. Длина = числу входов.")]
    [SerializeField] private bool[] target;

    [Header("Подсказка — показ правильного паттерна")]
    [Tooltip("Точки спавна ламп-подсказок (по одной на вход, в том же порядке).")]
    [SerializeField] private Transform[] hintAnchors;
    [Tooltip("Префаб 'включённой' лампы (вход должен быть запитан).")]
    [SerializeField] private GameObject onLampPrefab;
    [Tooltip("Префаб 'выключенной' лампы (вход должен быть обесточен).")]
    [SerializeField] private GameObject offLampPrefab;

    [Header("События (опц.)")]
    public UnityEvent onSolved;     // паттерн совпал (один раз за смену состояния)
    public UnityEvent onUnsolved;   // совпадение пропало

    public PowerNode Output => output;

    private bool _wasSolved;

    void Start() => SpawnHints();

    // Спавнит лампы-подсказки по конфигу target: on-лампа там, где вход должен
    // быть запитан, off-лампа — где обесточен.
    void SpawnHints()
    {
        if (hintAnchors == null || target == null) return;

        int n = Mathf.Min(hintAnchors.Length, target.Length);
        for (int i = 0; i < n; i++)
        {
            var anchor = hintAnchors[i];
            if (anchor == null) continue;

            var prefab = target[i] ? onLampPrefab : offLampPrefab;
            if (prefab != null)
                Instantiate(prefab, anchor.position, anchor.rotation, anchor);
        }
    }

    // Чистая функция (вызывается много раз за пересчёт сети — без побочных эффектов).
    public bool Compute(HashSet<PowerNode> powered)
    {
        if (inputs == null || target == null || inputs.Length != target.Length || inputs.Length == 0)
            return false;

        for (int i = 0; i < inputs.Length; i++)
        {
            bool on = inputs[i] != null && powered.Contains(inputs[i]);
            if (on != target[i]) return false;
        }
        return true;
    }

    // Фронты для UnityEvent ловим по факту питания выхода (как PowerCable).
    void Update()
    {
        if (output == null) return;
        bool solved = output.IsPowered;
        if (solved == _wasSolved) return;

        _wasSolved = solved;
        if (solved) onSolved?.Invoke();
        else        onUnsolved?.Invoke();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Держим длину target равной числу входов — удобнее настраивать.
        if (inputs != null && (target == null || target.Length != inputs.Length))
            System.Array.Resize(ref target, inputs.Length);
    }

    // Превью паттерна в редакторе: зелёный шар = вход должен гореть, тёмный = выключен.
    void OnDrawGizmos()
    {
        if (hintAnchors == null || target == null) return;

        int n = Mathf.Min(hintAnchors.Length, target.Length);
        for (int i = 0; i < n; i++)
        {
            if (hintAnchors[i] == null) continue;
            Gizmos.color = target[i] ? new Color(0.3f, 1f, 0.4f) : new Color(0.1f, 0.1f, 0.1f);
            Gizmos.DrawSphere(hintAnchors[i].position, 0.08f);
        }
    }
#endif
}
