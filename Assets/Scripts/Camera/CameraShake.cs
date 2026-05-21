using System.Collections;
using UnityEngine;

/// <summary>
/// Встряска камеры. Добавь на GameObject камеры.
/// Вызывай: Camera.main.GetComponent&lt;CameraShake&gt;()?.Shake(intensity, duration).
///
/// Также работает как singleton: CameraShake.Instance?.Shake(0.1f, 0.2f).
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Coroutine _shakeRoutine;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Запустить встряску.
    /// </summary>
    /// <param name="intensity">Амплитуда в юнитах (0.05–0.2 для лёгкого удара).</param>
    /// <param name="duration">Длительность в секундах.</param>
    public void Shake(float intensity, float duration)
    {
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration); // затухание

            transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * intensity * t;
            yield return null;
        }

        transform.localPosition = originalPos;
        _shakeRoutine = null;
    }
}
