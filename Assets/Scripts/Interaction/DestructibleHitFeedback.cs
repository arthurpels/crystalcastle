using System.Collections;
using UnityEngine;

/// <summary>
/// Сочный фидбек на удар по разрушаемому объекту: партиклы в точке попадания,
/// "вдавливание" меша (punch-scale) и тряска камеры. Слушает DestructibleObject.OnHit.
///
/// Звук удара уже играет сам DestructibleObject (PlayHitEffect) — здесь не дублируем.
/// </summary>
[RequireComponent(typeof(DestructibleObject))]
public class DestructibleHitFeedback : MonoBehaviour
{
    [Header("Визуал (что 'вдавливать')")]
    [Tooltip("Меш-объект для punch-эффекта. Пусто = этот transform.")]
    [SerializeField] private Transform visual;

    [Header("Партиклы удара")]
    [Tooltip("Щепки/пыль/искры. Спавнятся в точке попадания, развёрнутые по нормали.")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private float vfxLifetime = 3f;

    [Header("Punch (вдавливание меша)")]
    [Range(0f, 0.5f)][SerializeField] private float punchScale = 0.12f;
    [Range(0.02f, 0.4f)][SerializeField] private float punchDuration = 0.12f;

    [Header("Тряска камеры")]
    [SerializeField] private float shakeIntensity = 0.06f;
    [SerializeField] private float shakeDuration = 0.12f;

    private DestructibleObject _destructible;
    private Vector3 _baseScale;
    private Coroutine _punch;

    void Awake()
    {
        _destructible = GetComponent<DestructibleObject>();
        if (visual == null) visual = transform;
        _baseScale = visual.localScale;
    }

    void OnEnable()  => _destructible.OnHit += HandleHit;
    void OnDisable() => _destructible.OnHit -= HandleHit;

    private void HandleHit(Vector3 point, Vector3 normal)
    {
        // 1. Партиклы в точке попадания, ориентированы наружу по нормали.
        if (hitVfxPrefab != null)
        {
            var fx = Instantiate(hitVfxPrefab, point, Quaternion.LookRotation(normal));
            Destroy(fx, vfxLifetime);
        }

        // 2. Punch-scale: быстрое сжатие меша и возврат.
        if (_punch != null) StopCoroutine(_punch);
        _punch = StartCoroutine(PunchRoutine());

        // 3. Тряска камеры.
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(shakeIntensity, shakeDuration);
    }

    private IEnumerator PunchRoutine()
    {
        // Лёгкое сплющивание по вертикали + раздутие по горизонтали, 0→пик→0.
        Vector3 squashed = new Vector3(
            _baseScale.x * (1f + punchScale),
            _baseScale.y * (1f - punchScale),
            _baseScale.z * (1f + punchScale));

        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.Sin(Mathf.Clamp01(t / punchDuration) * Mathf.PI);
            visual.localScale = Vector3.LerpUnclamped(_baseScale, squashed, s);
            yield return null;
        }
        visual.localScale = _baseScale;
        _punch = null;
    }
}
