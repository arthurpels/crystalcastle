using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeaterThawZone : MonoBehaviour
{
    [SerializeField] private PowerableHeater heater;
    [SerializeField] private float thawDelay = 2f;
    [SerializeField] private LayerMask enemyLayer = ~0; // All layers by default

    private readonly HashSet<FrozenEnemy> _enemies = new();
    private Collider _col;
    private bool _wasHeating;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;
    }

    void Start()
    {
        if (heater != null)
        {
            heater.OnHeaterStateChanged += OnHeaterChanged;
            // Если обогреватель уже включён при старте — сканируем
            if (heater.IsHeating)
                ScanAndThawAll();
        }
    }

    void OnDestroy()
    {
        if (heater != null)
            heater.OnHeaterStateChanged -= OnHeaterChanged;
    }

    void OnHeaterChanged(bool heating)
    {
        if (heating && !_wasHeating)
        {
            ScanAndThawAll();
        }
        _wasHeating = heating;
    }

    /// <summary>
    /// Сканирует всю зону коллайдера и размораживает всех врагов внутри
    /// </summary>
    void ScanAndThawAll()
    {
        // Определяем мир-пространство параметры OverlapBox
        Bounds bounds = _col.bounds;
        Vector3 center = bounds.center;
        Vector3 halfExtents = bounds.extents;

        // Для BoxCollider точнее использовать его самого:
        if (_col is BoxCollider box)
        {
            center = transform.TransformPoint(box.center);
            halfExtents = Vector3.Scale(box.size * 0.5f, transform.lossyScale);
        }

        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, enemyLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out FrozenEnemy enemy))
            {
                if (!_enemies.Contains(enemy))
                    _enemies.Add(enemy);

                enemy.Thaw(thawDelay);
            }
        }
    }

    // --- Для врагов, которые входят в зону ПОСЛЕ включения ---
    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FrozenEnemy enemy) && !_enemies.Contains(enemy))
        {
            _enemies.Add(enemy);
            // Если обогреватель уже греет — размораживаем сразу
            if (heater != null && heater.IsHeating)
                enemy.Thaw(thawDelay);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FrozenEnemy enemy))
            _enemies.Remove(enemy);
    }

    void OnDrawGizmos()
    {
        if (_col == null) _col = GetComponent<Collider>();
        if (_col is BoxCollider box)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}