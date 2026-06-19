using UnityEngine;

/// <summary>
/// Триггер-зона концовки: как игрок входит — запускается выбранная концовка
/// через GameEnding (свой текст у каждой). Удобно для «сбежал из лаборатории →
/// концовка Уничтожение». Тип концовки выбирается в инспекторе.
/// </summary>
[RequireComponent(typeof(Collider))]
public class EndingTriggerZone : MonoBehaviour
{
    [Tooltip("Какую концовку запустить при входе игрока.")]
    [SerializeField] private GameEnding.EndingType ending = GameEnding.EndingType.Sacrifice;

    [SerializeField] private string playerTag = "Player";
    [Tooltip("Сработать только один раз.")]
    [SerializeField] private bool once = true;

    private bool _fired;

    private void Reset()  => GetComponent<Collider>().isTrigger = true;
    private void Awake()  => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (once && _fired) return;
        if (!other.CompareTag(playerTag)) return;

        _fired = true;

        if (GameEnding.Instance != null)
            GameEnding.Instance.Trigger(ending);
        else
            Debug.LogWarning("[EndingTriggerZone] GameEnding.Instance == null — нет объекта GameEnding в сцене.", this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color  = new Color(1f, 0.5f, 0.2f, 0.18f);
        Gizmos.matrix = transform.localToWorldMatrix;
        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        UnityEditor.Handles.Label(transform.position, $"⚑ Концовка: {ending}");
    }
#endif
}
