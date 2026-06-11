using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Глобальный лог свершившихся фактов игры (нарративные метки, прогресс).
/// — Raise(id): отметить, что событие произошло.
/// — Has(id):   спросить, происходило ли оно когда-либо.
/// — OnEvent:   статическое событие для слушателей (NarrativeTrigger и т.п.).
///
/// Хранит ДЕДУПЛИЦИРОВАННОЕ множество id, поэтому не растёт от времени игры —
/// потолок = число разных меток, заложенных в игру. Правило: id — конечный
/// словарь, заданный на этапе дизайна. НИКАКИХ счётчиков/GUID в строке id
/// (факты про конкретный инстанс храни в его собственном ISaveable).
///
/// Требует SaveableIdentity на том же GameObject, иначе не сохранится.
/// </summary>
public class GameEventLog : MonoBehaviour, ISaveable
{
    public static GameEventLog Instance { get; private set; }

    /// <summary>(id, isNew). isNew=false — событие уже происходило ранее.</summary>
    public static event Action<string, bool> OnEvent;

    private readonly HashSet<string> _fired = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Отметить событие. Повторный Raise того же id — no-op (isNew=false).</summary>
    public void Raise(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool isNew = _fired.Add(id);
        OnEvent?.Invoke(id, isNew);
    }

    /// <summary>Случалось ли событие хотя бы раз.</summary>
    public bool Has(string id) => _fired.Contains(id);

    // ── ISaveable ──────────────────────────────────────────────────────────

    [Serializable] private struct LogData { public List<string> ids; }

    public string CaptureState() =>
        JsonUtility.ToJson(new LogData { ids = new List<string>(_fired) });

    public void RestoreState(string json)
    {
        _fired.Clear();
        var d = JsonUtility.FromJson<LogData>(json);
        if (d.ids != null) _fired.UnionWith(d.ids);
        // Намеренно НЕ фаерим OnEvent: реакции (хинты) не должны проигрываться заново.
    }
}
