using UnityEngine;

public class ScenarioTrigger : MonoBehaviour
{
    [Header("What to disable")]
    [SerializeField] private PowerBreaker mainBreaker; // щиток света + дверей
    [SerializeField] private Door[] doorsToSeal;

    [Header("What stays on")]
    [SerializeField] private PowerableHeater emergencyHeater; // остаётся на другой ноде

    [Header("Settings")]
    [SerializeField] private bool destroyAfterTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // 1. Обесточить основную сеть (свет, двери)
        mainBreaker?.Trip();

        // 2. Принудительно закрыть двери
        foreach (var door in doorsToSeal)
            door?.ForceClose();

        // 3. Обогреватель остаётся включенным (он на отдельной ноде/генераторе)
        // Враги в зоне обогревателя начнут оттаивать через HeaterThawZone

        if (destroyAfterTrigger) Destroy(gameObject);
    }
}