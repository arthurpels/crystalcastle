using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewSurface", menuName = "Game/Surface Data")]
public class SurfaceData : ScriptableObject
{
    [Header("Влияние на движение")]
    [Tooltip("Множитель скорости. 1.0 = норма, 0.5 = в 2 раза медленнее")]
    public float speedMultiplier = 1.0f;

    [Tooltip("Сцепление с поверхностью. 0.0 = полное скольжение (лёд), 1.0 = максимальное трение")]
    [Range(0f, 1f)]
    public float gripFactor = 1.0f;

    [Header("Приоритет и интеграция")]
    [Tooltip("Чем выше число, тем приоритетнее поверхность при наложении зон")]
    public int priority = 1;

    [Tooltip("Метка для системы звуков шагов (например: ice, snow, grass)")]
    public string audioTag = "default";

    [Tooltip("Метка для системы частиц (брызги, снежная пыль и т.д.)")]
    public string vfxTag = "default";
}
