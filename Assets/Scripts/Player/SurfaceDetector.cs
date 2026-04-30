using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Глобальный детектор поверхностей.
/// Работает в паре с SurfaceColliderMapper на геометрии уровня.
/// </summary>
public class SurfaceDetector : MonoBehaviour
{
    [Header("Scan Settings")]
    [Tooltip("Как часто проверять окружение (сек)")]
    [Range(0.1f, 1f)] [SerializeField] private float scanInterval = 0.2f;
    
    [Tooltip("Радиус поиска поверхностей вокруг игрока")]
    [SerializeField] private float scanRadius = 0.7f;
    [SerializeField] private float scanOffset = -0.5f;
    
    [Tooltip("Слои, на которых могут быть поверхности")]
    [SerializeField] private LayerMask surfaceLayers;

    // Статический реестр коллайдеров с поверхностями
    private static readonly HashSet<SurfaceColliderMapper> _surfaceColliders = new();
    public static void RegisterSurfaceCollider(SurfaceColliderMapper mapper) => _surfaceColliders.Add(mapper);
    public static void UnregisterSurfaceCollider(SurfaceColliderMapper mapper) => _surfaceColliders.Remove(mapper);

    private MovementModifierStack _modifierStack;
    private readonly HashSet<SurfaceData> _detectedSurfaces = new();
    private float _scanTimer;

    private void Awake() => _modifierStack = GetComponent<MovementModifierStack>();

    private void Update()
    {
        _scanTimer -= Time.deltaTime;
        if (_scanTimer <= 0f)
        {
            ScanNearbySurfaces();
            _scanTimer = scanInterval;
        }
    }

    private void ScanNearbySurfaces()
    {
        if (_modifierStack == null) return;

        // 1. Находим все коллайдеры в радиусе
        Vector3 spherePosition = transform.position + Vector3.up * scanOffset;
        var colliders = Physics.OverlapSphere(spherePosition, scanRadius, surfaceLayers);
        
        // 2. Собираем поверхности из них
        var newSurfaces = new HashSet<SurfaceData>();
        foreach (var col in colliders)
        {
            if (col.TryGetComponent(out SurfaceColliderMapper mapper) && mapper.surfaceData != null)
                newSurfaces.Add(mapper.surfaceData);
        }

        // 3. Синхронизируем со стеком: добавляем новые, удаляем исчезнувшие
        foreach (var surface in newSurfaces)
            if (!_detectedSurfaces.Contains(surface))
                _modifierStack.AddSurface(surface);
        
        foreach (var surface in _detectedSurfaces)
            if (!newSurfaces.Contains(surface))
                _modifierStack.RemoveSurface(surface);

        _detectedSurfaces.Clear();
        _detectedSurfaces.UnionWith(newSurfaces);
    }

    // Отладка
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Vector3 spherePosition = transform.position + Vector3.up * scanOffset;
        Gizmos.DrawSphere(spherePosition, scanRadius);
    }
    #endif
}