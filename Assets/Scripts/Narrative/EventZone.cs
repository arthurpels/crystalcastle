using UnityEngine;

/// <summary>
/// Триггер-зона: когда игрок входит, делает GameEventLog.Raise(eventId).
/// Дальше реакцию ловит NarrativeTrigger с тем же eventId.
///
/// Сам факт «зона была пройдена» персистентен через лог, поэтому after-load
/// повторно не сработает (Raise вернёт isNew=false). Доп. локальная защита
/// _fired — чтобы не дёргать Raise каждый кадр пребывания в зоне.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EventZone : MonoBehaviour
{
    [Tooltip("ID события, которое поднимается при входе игрока.")]
    [SerializeField] private string eventId;

    [Tooltip("Тег объекта-игрока.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Сработать один раз за сессию (не поднимать Raise повторно).")]
    [SerializeField] private bool once = true;

    private bool _fired;

    void Reset()
    {
        // Удобство: коллайдер сразу делаем триггером при добавлении компонента.
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (once && _fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;
        GameEventLog.Instance?.Raise(eventId);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color  = new Color(0.4f, 0.8f, 1f, 0.15f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        UnityEditor.Handles.Label(transform.position, $"⚑ {eventId}");
    }
#endif
}
