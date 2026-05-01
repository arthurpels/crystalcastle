using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Глобальный детектор поверхностей.
/// Работает в паре с SurfaceColliderMapper на геометрии уровня.
/// </summary>
public class SurfaceDetector : MonoBehaviour {
    [Header("Scan Settings")]
    [Tooltip("Как часто проверять окружение (сек)")]
    [Range(0.1f, 1f)][SerializeField] private float scanInterval = 0.2f;

    [Tooltip("Радиус поиска поверхностей вокруг игрока")]
    [SerializeField] private float scanRadius = 0.7f;
    [SerializeField] private float scanOffset = -0.5f;

    [Tooltip("Слои, на которых могут быть поверхности")]
    [SerializeField] private LayerMask surfaceLayers;

    private static readonly HashSet<SurfaceColliderMapper> _surfaceColliders = new();
    public static void RegisterSurfaceCollider(SurfaceColliderMapper mapper) => _surfaceColliders.Add(mapper);
    public static void UnregisterSurfaceCollider(SurfaceColliderMapper mapper) => _surfaceColliders.Remove(mapper);



    public SurfaceData CurrentSurface { get; private set; }
    public SurfaceData PreviousSurface { get; private set; }

    public event Action<SurfaceData, SurfaceData> OnSurfaceChanged;


    private Collider[] detectedColliders = new Collider[5];
    private float _scanTimer;

    private void Update() {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f) {
            DetectSurface();
            _scanTimer = scanInterval;
        }
    }

    private void DetectSurface() {
        Vector3 spherePosition = transform.position + Vector3.up * scanOffset;

        int countColliders = Physics.OverlapSphereNonAlloc(spherePosition, scanRadius, detectedColliders, surfaceLayers);

        SurfaceData foundSurface = null;

        for (int i = 0; i < countColliders; i++) {
            Collider collider = detectedColliders[i];
            if (collider.TryGetComponent(out SurfaceColliderMapper mapper) && mapper.surfaceData != null)
                foundSurface = mapper.surfaceData;
        }

        if (foundSurface == null)
            foundSurface = GetDefaultSurface();

        SetSurface(foundSurface);
    }
    private void SetSurface(SurfaceData newSurface) {
        PreviousSurface = CurrentSurface;
        CurrentSurface = newSurface;

        OnSurfaceChanged?.Invoke(CurrentSurface, PreviousSurface);
    }

    protected virtual SurfaceData GetDefaultSurface() => null;

    // Отладка
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Vector3 spherePosition = transform.position + Vector3.up * scanOffset;
        Gizmos.DrawSphere(spherePosition, scanRadius);
    }
#endif
}