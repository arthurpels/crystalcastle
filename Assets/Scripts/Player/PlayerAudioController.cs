using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Проигрывает звуки шагов на основе поверхности под ногами.
/// Вызывается через AnimationEvent "OnFootstep" из анимаций Starter Assets.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerAudioController : MonoBehaviour {
    public enum AudioEventType { Footstep, Land } // Расширяй по мере необходимости

    [SerializeField] private SurfaceDetector surfaceDetector;
    [SerializeField] private MovementController movementController;

    [Range(0f, 1.2f)][SerializeField] private float pitchVariation = 0.08f;
    [Range(0f, 1.0f)][SerializeField] private float volumeVariation = 0.15f;

    [SerializeField] private List<SurfaceAudioBank> banks = new();
    [SerializeField] private AudioClip[] defaultFootstepClips;
    [SerializeField] private AudioClip[] defaultLandClips;

    private AudioSource _source;
    private Dictionary<AudioEventType, Dictionary<string, AudioClip[]>> _cache;

    [System.Serializable]
    public class SurfaceAudioBank {
        [Tooltip("Должен совпадать с audioTag в SurfaceData")]
        public string surfaceTag;
        public AudioClip[] footstepClips;
        public AudioClip[] landClips;
        // future: public AudioClip[] jumpClips;
        // future: public AudioClip[] vaultClips;
    }

    private void Awake() {
        _source = GetComponent<AudioSource>();

        if (surfaceDetector == null) surfaceDetector = FindObjectOfType<SurfaceDetector>();
        if (movementController == null) movementController = GetComponent<MovementController>();

        BuildCache();
    }
    private void BuildCache() {
        _cache = new Dictionary<AudioEventType, Dictionary<string, AudioClip[]>>();

        foreach (AudioEventType type in Enum.GetValues(typeof(AudioEventType)))
            _cache[type] = new Dictionary<string, AudioClip[]>();

        foreach (var bank in banks) {
            if (string.IsNullOrEmpty(bank.surfaceTag)) continue;

            if (bank.footstepClips != null && bank.footstepClips.Length > 0)
                _cache[AudioEventType.Footstep][bank.surfaceTag] = bank.footstepClips;

            if (bank.landClips != null && bank.landClips.Length > 0)
                _cache[AudioEventType.Land][bank.surfaceTag] = bank.landClips;
        }
    }

    public void OnFootstep() => PlaySurfaceAudio(AudioEventType.Footstep);
    public void OnLand() => PlaySurfaceAudio(AudioEventType.Land);


    private void PlaySurfaceAudio(AudioEventType eventType) {
        // 1. Определяем тег поверхности
        string tag = surfaceDetector?.CurrentSurface?.audioTag ?? "default";

        // 2. Ищем клипы в кеше
        AudioClip[] clips = GetClips(eventType, tag);

        // 3. Фоллбэк на дефолт, если тег не найден
        if (clips == null || clips.Length == 0)
            clips = GetClips(eventType, "default");

        // 4. Проигрываем
        if (clips != null && clips.Length > 0) {
            AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            ApplyVariation(eventType);
            _source.PlayOneShot(clip);
        }
    }
    
    private AudioClip[] GetClips(AudioEventType eventType, string tag)
    {
        if (_cache.TryGetValue(eventType, out var typeDict) && 
            typeDict.TryGetValue(tag, out var clips))
            return clips;
        return null;
    }

    private void ApplyVariation(AudioEventType eventType)
    {
        // Базовая рандомизация
        _source.pitch = UnityEngine.Random.Range(1f - pitchVariation, 1f + pitchVariation);
        _source.volume = UnityEngine.Random.Range(1f - volumeVariation, 1f);

        // Спец-логика для приземления: громкость зависит от силы удара
        if (eventType == AudioEventType.Land && movementController != null)
        {
            float impactForce = Mathf.Abs(movementController.VerticalVelocity);
            // Нормализуем: 0..15 m/s -> 0.3..1.0 volume
            float impactVolume = Mathf.Clamp01(impactForce / 15f);
            impactVolume = Mathf.Lerp(0.3f, 1f, impactVolume); // Мин. громкость 30% даже при мягком приземлении
            _source.volume *= impactVolume;
        }
    }

    // Отладка: видим радиус проверки в сцене
#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.3f);
    }
#endif
}