using System.Collections.Generic;
using UnityEngine;

public enum LogicGateType { AND, OR, XOR }

/// <summary>
/// Переключаемый логический элемент для головоломки: AND / OR / XOR.
/// Эти три выдают сигнал только когда их входы запитаны — поэтому не нужно
/// придумывать «откуда сигнал»: всё течёт от генератора по сети.
///
/// Игрок переключает тип взаимодействием (Interact). Можно также менять из кода
/// через свойство Type. Обрабатывается в PowerNetwork.Evaluate().
/// </summary>
public class LogicGate : MonoBehaviour, IPowerGate, IInteractable, ISaveable
{
    [Header("Grid")]
    [SerializeField] private PowerNode[]   inputs;
    [SerializeField] private PowerNode     output;
    [SerializeField] private LogicGateType type = LogicGateType.AND;

    [System.Serializable]
    public struct GatePanel
    {
        public Renderer renderer;          // табличка с надписью (AND/OR/XOR)
        public Material activeMaterial;     // подсвеченный (когда этот тип выбран)
        public Material inactiveMaterial;   // тусклый (когда выбран другой тип)
    }

    [Header("Visual — таблички типа")]
    [Tooltip("По индексу = типу: [0]=AND, [1]=OR, [2]=XOR. Активному типу — его " +
             "active-материал, остальным — inactive. Все таблички видны всегда.")]
    [SerializeField] private GatePanel[] panels;

    public PowerNode Output => output;

    public LogicGateType Type
    {
        get => type;
        set
        {
            if (type == value) return;
            type = value;
            UpdateVisual();
            PowerNetwork.Instance?.Evaluate();
        }
    }

    void Start() => UpdateVisual();

    public bool Compute(HashSet<PowerNode> powered)
    {
        if (inputs == null || inputs.Length == 0) return false;

        int on = 0, total = 0;
        foreach (var n in inputs)
        {
            if (n == null) continue;
            total++;
            if (powered.Contains(n)) on++;
        }
        if (total == 0) return false;

        switch (type)
        {
            case LogicGateType.AND: return on == total;     // все
            case LogicGateType.OR:  return on > 0;          // хотя бы один
            case LogicGateType.XOR: return (on & 1) == 1;   // нечётное число (обобщённый XOR)
            default:                return false;
        }
    }

    // ── Переключение игроком ─────────────────────────────────────────────────
    public void Interact()
    {
        Type = (LogicGateType)(((int)type + 1) % 3);
    }

    public string PromptText => $"Логика: {type} (переключить)";

    void UpdateVisual()
    {
        // Активному типу — active-материал, остальным — inactive (индекс = (int)type).
        if (panels == null) return;
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].renderer == null) continue;
            panels[i].renderer.sharedMaterial =
                (i == (int)type) ? panels[i].activeMaterial : panels[i].inactiveMaterial;
        }
    }

    // ── ISaveable ──────────────────────────────────────────────────────────────
    [System.Serializable]
    private struct GateSaveData { public int type; }

    public string CaptureState() =>
        JsonUtility.ToJson(new GateSaveData { type = (int)type });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<GateSaveData>(json);
        type = (LogicGateType)d.type;
        UpdateVisual();
        // Evaluate() вызовет SaveManager в конце загрузки.
    }
}
