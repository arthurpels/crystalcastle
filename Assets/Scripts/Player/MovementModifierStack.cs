using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/// <summary>
/// Собирает SurfaceData из зон, комбинирует их и применяет к MovementController.
/// Работает с твоими ScriptableObject — ничего менять не нужно.
/// </summary>
[RequireComponent(typeof(MovementController))]
public class MovementModifierStack : MonoBehaviour {
    [Header("Сглаживание")]
    [Tooltip("Время плавного перехода между значениями (сек)")]
    [Range(0f, 2f)][SerializeField] private float transitionTime = 0.3f;


    [Header("Базовые настройки (Воздух)")]
    [Tooltip("Множитель скорости по умолчанию (когда нет поверхностей)")]
    [Range(0.1f, 3f)][SerializeField] private float baseSpeedMultiplier = 1.0f;

    [Tooltip("Базовое сцепление (в воздухе). 0.2 = небольшой контроль, 0.0 = полная инерция")]
    [Range(0f, 1f)][SerializeField] private float baseGripMultiplier = 0.2f;

    // Публичный доступ для отладки и внешних систем
    public IReadOnlyList<SurfaceData> ActiveSurfaces => _activeSurfaces.AsReadOnly();
    public float CurrentSpeedMultiplier { get; private set; } = 1f;
    public float CurrentGripMultiplier { get; private set; } = 1f;

    private SurfaceDetector _surfaceDetector;
    private MovementController _controller;
    private readonly List<SurfaceData> _activeSurfaces = new();

    // Плавная интерполяция
    private float _targetSpeedMult = 1f;
    private float _targetGripMult = 1f;
    private float _currentSpeedMult = 1f;
    private float _currentGripMult = 1f;

    private void Awake() {
        _controller = GetComponent<MovementController>();
        _surfaceDetector = FindObjectOfType<SurfaceDetector>();
    }
    private void Update() => ApplyModifiers();

    

    // /// <summary>Добавить поверхность. Возвращает успешность.</summary>
    // public bool AddSurface(SurfaceData surface) {
    //     if (surface == null) return false;
    //     if (_activeSurfaces.Contains(surface)) return false;

    //     _activeSurfaces.Add(surface);
    //     RecalculateTargets();
    //     return true;
    // }

    // /// <summary>Удалить поверхность.</summary>
    // public bool RemoveSurface(SurfaceData surface) {
    //     bool removed = _activeSurfaces.Remove(surface);
    //     if (removed) RecalculateTargets();
    //     return removed;
    // }

    // /// <summary>Удалить все поверхности с указанным приоритетом (для зон одного типа)</summary>
    // public void RemoveSurfacesByPriority(int priority) {
    //     bool changed = _activeSurfaces.RemoveAll(s => s.priority == priority) > 0;
    //     if (changed) RecalculateTargets();
    // }

    // /// <summary>Очистить всё (смерть, ресет сцены)</summary>
    // public void ClearAll() {
    //     _activeSurfaces.Clear();
    //     _targetSpeedMult = _targetGripMult = 1f;
    // }

    // // ==================== ЛОГИКА КОМБИНИРОВАНИЯ ====================

    // private void RecalculateTargets() {
    //     if (_activeSurfaces.Count == 0) {
    //         _targetSpeedMult = baseSpeedMultiplier;
    //         _targetGripMult = baseGripMultiplier;
    //         _currentSpeedMult = _targetSpeedMult;
    //         _currentGripMult = _targetGripMult;
    //         return;
    //     }

    //     // Сортировка: сначала высокий приоритет
    //     var sorted = _activeSurfaces.OrderByDescending(s => s.priority).ToList();

    //     if (sorted.First().priority != sorted.Last().priority) {
    //         _targetSpeedMult = sorted.First().speedMultiplier;
    //         _targetGripMult = sorted.First().gripFactor;
    //         // _targetGripMult = ConvertGripToMultiplier(exclusive.gripFactor);
    //         return;
    //     } else {
    //         float speed = 1f, grip = 1f;
    //         foreach (var surface in sorted) {
    //             speed *= surface.speedMultiplier;
    //             grip *= surface.gripFactor;
    //         }

    //         _targetSpeedMult = Mathf.Clamp(speed, 0.001f, 3f);
    //         _targetGripMult = Mathf.Clamp(grip, 0f, 1f);
    //     }

    // }
    private void ApplyModifiers() {
        _targetSpeedMult = baseSpeedMultiplier;
        _targetGripMult = baseGripMultiplier;

        // Если есть поверхность — берём её множители
        if (_surfaceDetector != null && _surfaceDetector.CurrentSurface != null)
        {
            var surface = _surfaceDetector.CurrentSurface;
            _targetSpeedMult = surface.speedMultiplier;
            _targetGripMult =  surface.gripFactor;
        }

        if (transitionTime > 0f) {
            _currentSpeedMult = Mathf.Lerp(_currentSpeedMult, _targetSpeedMult, Time.deltaTime / transitionTime);
            _currentGripMult = Mathf.Lerp(_currentGripMult, _targetGripMult, Time.deltaTime / transitionTime);
        } else {
            _currentSpeedMult = _targetSpeedMult;
            _currentGripMult = _targetGripMult;
        }

        // Dead zone
        // if (Mathf.Abs(_currentSpeedMult - 1f) < deadZone) _currentSpeedMult = 1f;
        // if (Mathf.Abs(_currentGripMult - 1f) < deadZone) _currentGripMult = 1f;

        // Применяем в контроллер
        _controller.SpeedMultiplier = _currentSpeedMult;
        _controller.GripMultiplier = _currentGripMult;

        // Кэш для отладки
        CurrentSpeedMultiplier = _currentSpeedMult;
        CurrentGripMultiplier = _currentGripMult;
    }


    private void OnDrawGizmos() {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2.5f);
        Handles.Label(transform.position + Vector3.up * 2.5f,
            $"Speed: {_currentSpeedMult:F2}\nGrip: {_currentGripMult:F2}\nSurfaces: {_activeSurfaces.Count}");

        // Рисуем активные поверхности списком
        if (_activeSurfaces.Count > 0) {
            string labels = "";
            foreach (var s in _activeSurfaces.OrderByDescending(x => x.priority))
                labels += $"{s.name} (×{s.speedMultiplier}, grip:{s.gripFactor})\n";
            Handles.Label(transform.position + Vector3.up * 1.5f, labels);
        }
    }
}