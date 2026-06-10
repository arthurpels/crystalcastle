using UnityEngine;

/// <summary>
/// Лёгкое мерцание яркости — имитация фосфорного ЭЛТ-экрана.
/// Повесь на объект с CanvasGroup (например, корень терминальной панели).
/// Работает на unscaled time, поэтому мерцает и когда игра на паузе.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CRTFlicker : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float baseAlpha = 1f;
    [Tooltip("Глубина мерцания (0 = выключено)")]
    [SerializeField, Range(0f, 0.2f)] private float flickerAmount = 0.04f;
    [SerializeField] private float speed = 14f;
    [Tooltip("Редкие резкие 'провалы' сигнала")]
    [SerializeField, Range(0f, 0.1f)] private float dropoutChance = 0.01f;
    [SerializeField, Range(0f, 0.6f)] private float dropoutAmount = 0.25f;

    private CanvasGroup _cg;
    private float _seed;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        _seed = Random.value * 100f;
    }

    private void OnEnable()
    {
        if (_cg != null) _cg.alpha = baseAlpha;
    }

    private void Update()
    {
        if (_cg == null) return;

        float n = Mathf.PerlinNoise(Time.unscaledTime * speed + _seed, 0f);
        float a = baseAlpha - n * flickerAmount;

        if (Random.value < dropoutChance)
            a -= Random.value * dropoutAmount;

        _cg.alpha = Mathf.Clamp01(a);
    }
}
