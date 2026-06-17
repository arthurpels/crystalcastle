using System.Collections;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Встряска камеры через Cinemachine (CM2). Прямой сдвиг трансформа не работает —
/// Cinemachine каждый кадр перезаписывает позицию камеры, поэтому трясём через
/// Basic Multi Channel Perlin (шум) на виртуальной камере.
///
/// Настройка:
///   1. На vcam (cm) в инспекторе: Noise → Basic Multi Channel Perlin.
///   2. Назначь Noise Profile (например встроенный "6D Shake"; если нет —
///      Create → Cinemachine → Noise Settings).
///   3. Amplitude/Frequency Gain на покое = 0 (или лёгкое значение для качания —
///      оно сохранится как база, тряска добавится поверх).
///   4. Назначь vcam в поле этого компонента (или оставь пусто — найдётся сам).
///
/// API не изменился: CameraShake.Instance.Shake(intensity, duration).
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Tooltip("Виртуальная камера Cinemachine (cm). Пусто = найти в сцене.")]
    [SerializeField] private CinemachineVirtualCamera vcam;

    [Tooltip("Множитель: intensity * это = добавка к Amplitude Gain у Perlin.")]
    [SerializeField] private float amplitudeScale = 20f;

    [Tooltip("Частота тряски (Frequency Gain) на время встряски.")]
    [SerializeField] private float shakeFrequency = 2.0f;

    private CinemachineBasicMultiChannelPerlin _perlin;
    private float _baseAmplitude;   // базовое покачивание (сохраняем)
    private float _baseFrequency;
    private Coroutine _shakeRoutine;

    private void Awake()
    {
        Instance = this;

        if (vcam == null) vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam != null)
            _perlin = vcam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        if (_perlin == null)
        {
            Debug.LogWarning(
                "[CameraShake] На vcam нет Basic Multi Channel Perlin. " +
                "Добавь Noise → Basic Multi Channel Perlin и назначь Noise Profile.", this);
            return;
        }

        // Запоминаем базу — тряска будет добавляться поверх и возвращаться к ней.
        _baseAmplitude = _perlin.m_AmplitudeGain;
        _baseFrequency = _perlin.m_FrequencyGain;
    }

    /// <param name="intensity">Сила (как раньше: 0.05–0.2 для лёгкого удара).</param>
    /// <param name="duration">Длительность в секундах.</param>
    public void Shake(float intensity, float duration)
    {
        if (_perlin == null || duration <= 0f) return;
        if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
        _shakeRoutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        float addAmplitude = intensity * amplitudeScale;
        _perlin.m_FrequencyGain = Mathf.Max(_baseFrequency, shakeFrequency);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float falloff = 1f - (elapsed / duration); // затухание к базе
            _perlin.m_AmplitudeGain = _baseAmplitude + addAmplitude * falloff;
            yield return null;
        }

        // Возврат к базовому покачиванию.
        _perlin.m_AmplitudeGain = _baseAmplitude;
        _perlin.m_FrequencyGain = _baseFrequency;
        _shakeRoutine = null;
    }
}
