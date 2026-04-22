using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SurfaceColliderMapper : MonoBehaviour
{
    [Tooltip("Данные поверхности, которые применяются при контакте или сканировании")]
    public SurfaceData surfaceData;

    // Валидация на старте, чтобы не забыть назначить ассет
    private void Reset()
    {
        if (surfaceData == null)
            Debug.LogWarning($"[SurfaceMapper] На объекте {name} не назначен SurfaceData!", this);
    }
}
