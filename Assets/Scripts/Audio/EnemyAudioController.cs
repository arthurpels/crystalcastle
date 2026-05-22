using System.Collections;
using UnityEngine;

/// <summary>
/// Управляет звуками отмороженного. Добавь на тот же GameObject что и SimpleEnemyAI.
/// Требует AudioSource (для дыхания/гула) — второй создаётся автоматически для ударов.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(SimpleEnemyAI))]
public class EnemyAudioController : MonoBehaviour {
    [Header("Idle / Patrol")]
    [Tooltip("Редкое, очень медленное дыхание (12-20 уд/мин)")]
    [SerializeField] private AudioClip[] idleBreathClips;
    [SerializeField] private float idleBreathInterval = 4f;   // секунд между вздохами
    [SerializeField] private float idleBreathVariation = 1.5f;

    [Header("Chase")]
    [Tooltip("Учащённое, тяжёлое дыхание")]
    [SerializeField] private AudioClip[] chaseBreathClips;
    [SerializeField] private float chaseBreathInterval = 1.8f;
    [SerializeField] private float chaseBreathVariation = 0.4f;

    [Header("Alert — момент обнаружения игрока")]
    [Tooltip("Короткий звук при переходе Idle→Chase")]
    [SerializeField] private AudioClip alertClip;

    [Header("Attack")]
    [Tooltip("Резкий звук удара/броска")]
    [SerializeField] private AudioClip[] attackClips;

    [Header("Ambient Hum")]
    [Tooltip("Низкочастотный протяжный гул (почти инфразвук) — зацикленный")]
    [SerializeField] private AudioClip ambientHumClip;
    [SerializeField][Range(0f, 1f)] private float humVolume = 0.25f;

    [Header("Volumes")]
    [SerializeField][Range(0f, 1f)] private float breathVolume = 0.6f;
    [SerializeField][Range(0f, 1f)] private float attackVolume = 0.9f;
    [SerializeField][Range(0.05f, 0.3f)] private float pitchVariation = 0.08f;

    [SerializeField] private AudioClip[] footstepSounds;

    [SerializeField] private AudioClip landSound;
    [SerializeField] private float footstepVolume = 0.4f;

    

    private AudioSource _breathSource;  // дыхание — меняется по состоянию
    private AudioSource _humSource;     // постоянный гул
    private AudioSource _oneShotSource; // удары, alert

    private SimpleEnemyAI _ai;
    private SimpleEnemyAI.State _prevState;

    private float _breathTimer;
    private Coroutine _breathRoutine;

    private void Awake() {
        _ai = GetComponent<SimpleEnemyAI>();

        _breathSource = GetComponent<AudioSource>();
        _oneShotSource = CreateSource("EnemyOneShot", attackVolume, false);
        _humSource = CreateSource("EnemyHum", humVolume, true);

        _breathSource.spatialBlend = 1f;
        _breathSource.rolloffMode = AudioRolloffMode.Linear;
        _breathSource.minDistance = 1f;
        _breathSource.maxDistance = 12f;
        _breathSource.playOnAwake = false;
        _breathSource.volume = breathVolume;

        if (ambientHumClip != null) {
            _humSource.clip = ambientHumClip;
            _humSource.Play();
        }
    }

    private void OnEnable() {
        _prevState = _ai.CurrentState;
        _breathRoutine = StartCoroutine(BreathLoop());
    }

    private void OnDisable() {
        if (_breathRoutine != null) StopCoroutine(_breathRoutine);
    }

    private void Update() {
        var state = _ai.CurrentState;
        if (state == _prevState) return;

        // Переход в Chase — звук обнаружения
        if (state == SimpleEnemyAI.State.Chase && _prevState != SimpleEnemyAI.State.Attack)
            PlayOneShot(alertClip, attackVolume);

        _prevState = state;
    }

    // Вызывай из SimpleEnemyAI при атаке: GetComponent<EnemyAudioController>()?.OnAttack();
    public void OnAttack() {
        if (attackClips == null || attackClips.Length == 0) return;
        PlayOneShot(attackClips[Random.Range(0, attackClips.Length)], attackVolume);
    }

    // ── Дыхательный цикл ────────────────────────────────────────────────────

    private IEnumerator BreathLoop() {
        while (true) {
            var state = _ai.CurrentState;

            AudioClip[] pool = state == SimpleEnemyAI.State.Chase ? chaseBreathClips : idleBreathClips;
            float interval = state == SimpleEnemyAI.State.Chase ? chaseBreathInterval : idleBreathInterval;
            float variation = state == SimpleEnemyAI.State.Chase ? chaseBreathVariation : idleBreathVariation;

            if (pool != null && pool.Length > 0) {
                _breathSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
                _breathSource.volume = Random.Range(breathVolume * 0.75f, breathVolume);
                _breathSource.PlayOneShot(pool[Random.Range(0, pool.Length)]);
            }

            yield return new WaitForSeconds(interval + Random.Range(-variation, variation));
        }
    }

    // ⚠️ Должно быть public и без параметров (или с одним string)
    public void OnFootstep()  // или public void OnFootstep(string eventName)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        PlayOneShot(
            footstepSounds[Random.Range(0, footstepSounds.Length)],
            footstepVolume
        );
    }

    public void OnLand()  // или public void OnFootstep(string eventName)
    {
        if (landSound == null) return;

        PlayOneShot(
            landSound,
            footstepVolume
        );
    }

    // ── Утилиты ──────────────────────────────────────────────────────────────

    private void PlayOneShot(AudioClip clip, float volume) {
        if (clip == null) return;
        _oneShotSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        _oneShotSource.PlayOneShot(clip, volume);
    }

    private AudioSource CreateSource(string label, float volume, bool loop) {
        var go = new GameObject(label);
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 1.4f; // высота головы
        var src = go.AddComponent<AudioSource>();
        src.volume = volume;
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 1f;
        src.maxDistance = 14f;
        return src;
    }
}
