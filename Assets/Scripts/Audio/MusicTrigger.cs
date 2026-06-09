using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Триггер смены музыки с приоритетом. Поставь на пустой GameObject с BoxCollider.
///
/// Если зоны перекрываются — побеждает зона с наибольшим Priority.
/// При выходе из зоны автоматически возвращается к следующей активной зоне.
///
/// Настройка:
///   1. Создай Empty GameObject, добавь BoxCollider (Is Trigger ставится автоматически).
///   2. Добавь этот компонент.
///   3. Назначь AudioClip в поле Track.
///   4. Задай Priority: глобальный эмбиент = 0, комнаты = 1, опасные зоны = 2, босс = 10.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MusicTrigger : MonoBehaviour
{
    [Header("Музыка")]
    [SerializeField] private AudioClip track;

    [Header("Приоритет")]
    [Tooltip("Чем выше число — тем важнее зона. При перекрытии побеждает зона с наибольшим приоритетом.")]
    [SerializeField] private int priority = 0;

    [Header("Поведение")]
    [Tooltip("Не переигрывать трек при повторном входе в зону")]
    [SerializeField] private bool playOnce = false;

    [Tooltip("Тег объекта который активирует триггер")]
    [SerializeField] private string playerTag = "Player";

    // Все зоны в которых игрок находится прямо сейчас (общий список для всех экземпляров)
    private static readonly List<MusicTrigger> _activeTriggers = new List<MusicTrigger>();

    private bool _hasPlayed;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnDestroy()
    {
        if (_activeTriggers.Remove(this))
            EvaluatePriority();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (track == null) return;
        if (playOnce && _hasPlayed) return;

        if (!_activeTriggers.Contains(this))
            _activeTriggers.Add(this);

        EvaluatePriority();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // Remove возвращает true только если элемент был в списке
        if (_activeTriggers.Remove(this))
            EvaluatePriority();
    }

    // ── Логика выбора победителя ──────────────────────────────────────────

    private static void EvaluatePriority()
    {
        if (_activeTriggers.Count == 0) return;
        if (AudioManager.Instance == null) return;

        MusicTrigger best = null;
        foreach (var t in _activeTriggers)
        {
            if (best == null || t.priority > best.priority)
                best = t;
        }

        if (best == null || best.track == null) return;

        AudioManager.Instance.PlayTrack(best.track);
        best._hasPlayed = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        // Цвет: зелёный (priority=0) → оранжевый (priority=5+)
        float t = Mathf.Clamp01(priority / 5f);
        Color c = Color.Lerp(new Color(0.2f, 0.8f, 0.4f), new Color(0.95f, 0.5f, 0.1f), t);

        Gizmos.color  = new Color(c.r, c.g, c.b, 0.20f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = new Color(c.r, c.g, c.b, 0.85f);
        Gizmos.DrawWireCube(col.center, col.size);

        if (track != null)
        {
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (col.size.y * 0.5f + 0.5f),
                $"♪ {track.name}  [P:{priority}]");
        }
    }
#endif
}
