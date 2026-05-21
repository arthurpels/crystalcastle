using UnityEngine;

/// <summary>
/// Одноразовый триггер сценария.
/// — При входе игрока: обесточивает сеть, запирает двери.
/// — При destroyAfterTrigger=true уничтожает себя (и регистрирует в SaveManager).
/// — При destroyAfterTrigger=false сохраняет флаг hasTriggered через ISaveable
///   (нужен SaveableIdentity на том же GameObject).
/// </summary>
public class ScenarioTrigger : MonoBehaviour, ISaveable
{
    [Header("What to disable")]
    [SerializeField] private PowerBreaker mainBreaker; // щиток света + дверей
    [SerializeField] private Door[] doorsToSeal;

    [Header("What stays on")]
    [SerializeField] private PowerableHeater emergencyHeater; // остаётся на другой ноде

    [Header("Settings")]
    [SerializeField] private bool destroyAfterTrigger = true;

    private bool _hasTriggered;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (_hasTriggered) return;  // защита от повторного срабатывания
        _hasTriggered = true;

        // 1. Обесточить основную сеть (свет, двери)
        mainBreaker?.Trip();

        // 2. Принудительно закрыть двери
        foreach (var door in doorsToSeal)
            door?.ForceClose();

        // 3. Обогреватель остаётся включенным (он на отдельной ноде/генераторе)
        // Враги в зоне обогревателя начнут оттаивать через HeaterThawZone

        if (destroyAfterTrigger)
        {
            SaveManager.Instance?.RegisterDestroyed(this);
            Destroy(gameObject);
        }
    }

    // ── ISaveable (актуально при destroyAfterTrigger = false) ─────────────

    [System.Serializable]
    private struct TriggerSaveData { public bool hasTriggered; }

    public string CaptureState() =>
        JsonUtility.ToJson(new TriggerSaveData { hasTriggered = _hasTriggered });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<TriggerSaveData>(json);
        _hasTriggered = d.hasTriggered;
    }
}