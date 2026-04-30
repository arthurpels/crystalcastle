using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurfaceColliderMapper : MonoBehaviour
{
    [Tooltip("Данные поверхности, которые применяются при контакте или сканировании")]
    public SurfaceData surfaceData;

    [Tooltip("Авто-поиск стека при старте (если не назначен вручную)")]
    [SerializeField] private bool autoFindStack = true;
    
    [Tooltip("Ссылка на стек модификаторов (опционально)")]
    [SerializeField] private MovementModifierStack modifierStack;

    private void Start()
    {
        // Авто-поиск, если не назначен вручную и включена опция
        if (modifierStack == null && autoFindStack)
            modifierStack = FindObjectOfType<MovementModifierStack>();
        
        // Валидация коллайдера
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            SurfaceDetector.RegisterSurfaceCollider(this);
    }

    private void OnDestroy()
    {
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            SurfaceDetector.UnregisterSurfaceCollider(this);
    }

    private void OnTriggerEnter(Collider other) => TryApplySurface(other, true);
    private void OnTriggerExit(Collider other) => TryApplySurface(other, false);

    private void TryApplySurface(Collider other, bool enter)
    {
        if (surfaceData == null || modifierStack == null) return;
        

        if (!IsPlayer(other)) return;

        if (enter)
            modifierStack.AddSurface(surfaceData);
        else
            modifierStack.RemoveSurface(surfaceData);
    }

    private bool IsPlayer(Collider other) => 
        other.CompareTag("Player") || other.GetComponent<MovementController>() != null;

    // Для отладки в редакторе
    

    // Валидация на старте, чтобы не забыть назначить ассет
    private void Reset()
    {
        if (surfaceData == null)
            Debug.LogWarning($"[SurfaceMapper] На объекте {name} не назначен SurfaceData!", this);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (surfaceData == null) return;
        
        // Цвет по типу поверхности
        Color color = surfaceData.gripFactor < 0.5f ? Color.cyan :  // лёд
                     surfaceData.gripFactor < 0.8f ? Color.blue :   // снег
                     surfaceData.speedMultiplier != 1f ? Color.yellow : Color.green;
        
        Gizmos.color = new Color(color.r, color.g, color.b, 0.3f);
        var bounds = GetComponent<Collider>()?.bounds;
        if (bounds.HasValue)
            Gizmos.DrawCube(bounds.Value.center, bounds.Value.size);
        
        Gizmos.color = color;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider>()?.bounds.size ?? Vector3.one);
        Handles.Label(transform.position, 
            $"{surfaceData.name}\n×{surfaceData.speedMultiplier} speed, grip:{surfaceData.gripFactor}");
    }
    #endif
}
