using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HeaterThawZone : MonoBehaviour
{
    [SerializeField] private PowerableHeater heater;
    [SerializeField] private float thawDelay = 2f;

    private readonly HashSet<FrozenEnemy> _enemiesInZone = new();

    void OnEnable()
    {
        if (heater != null)
            heater.OnHeaterStateChanged += OnHeaterChanged;
    }

    void OnDisable()
    {
        if (heater != null)
            heater.OnHeaterStateChanged -= OnHeaterChanged;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FrozenEnemy enemy) && !_enemiesInZone.Contains(enemy))
            _enemiesInZone.Add(enemy);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FrozenEnemy enemy))
            _enemiesInZone.Remove(enemy);
    }

    void OnHeaterChanged(bool heating)
    {
        if (!heating) return;
        foreach (var enemy in _enemiesInZone) {
            if (enemy != null) enemy.Thaw(thawDelay);
        }
    }
}