using System.Collections;
using UnityEngine;

/// <summary>
/// Синглтон для управления музыкой и SFX.
/// Добавь на отдельный GameObject "AudioManager" в каждую сцену,
/// либо один раз с DontDestroyOnLoad.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip[] playlist;
    [SerializeField][Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private float crossfadeDuration = 2f;
    [SerializeField] private bool shufflePlaylist = false;

    [Header("SFX")]
    [SerializeField][Range(0f, 1f)] private float sfxVolume = 1f;

    private AudioSource _musicA;
    private AudioSource _musicB;
    private AudioSource _sfxSource;

    private int _currentTrackIndex = -1;
    private bool _usingA = true;
    private Coroutine _crossfadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicA  = CreateSource("MusicA",  musicVolume, true);
        _musicB  = CreateSource("MusicB",  0f,          true);
        _sfxSource = CreateSource("SFX",   sfxVolume,   false);
    }

    private void Start()
    {
        if (playlist != null && playlist.Length > 0)
            PlayNextTrack();
    }

    // ── Публичное API ────────────────────────────────────────────────────────

    /// Воспроизвести конкретный клип SFX (вызывай из любого скрипта).
    public static void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        Instance._sfxSource.PlayOneShot(clip, volume * Instance.sfxVolume);
    }

    /// Воспроизвести SFX в мировой позиции (с затуханием по расстоянию).
    public static void PlaySFXAt(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (Instance == null || clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, volume * Instance.sfxVolume);
    }

    /// Сразу переключить музыку на конкретный трек (с кроссфейдом).
    public void PlayTrack(AudioClip clip)
    {
        if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
        _crossfadeRoutine = StartCoroutine(Crossfade(clip));
    }

    /// Остановить музыку.
    public void StopMusic() => StartCoroutine(FadeOut(ActiveMusic(), crossfadeDuration));

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        ActiveMusic().volume = musicVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        _sfxSource.volume = sfxVolume;
    }

    // ── Внутренняя логика ────────────────────────────────────────────────────

    private void Update()
    {
        // Автоматически переключаем трек когда текущий закончился
        if (playlist != null && playlist.Length > 0 && !ActiveMusic().isPlaying)
            PlayNextTrack();
    }

    private void PlayNextTrack()
    {
        if (playlist == null || playlist.Length == 0) return;

        if (shufflePlaylist)
            _currentTrackIndex = Random.Range(0, playlist.Length);
        else
            _currentTrackIndex = (_currentTrackIndex + 1) % playlist.Length;

        PlayTrack(playlist[_currentTrackIndex]);
    }

    private IEnumerator Crossfade(AudioClip newClip)
    {
        AudioSource fadeOut = ActiveMusic();
        AudioSource fadeIn  = InactiveMusic();

        fadeIn.clip   = newClip;
        fadeIn.volume = 0f;
        fadeIn.Play();
        _usingA = !_usingA;

        float elapsed = 0f;
        float startVol = fadeOut.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;
            fadeIn.volume  = Mathf.Lerp(0f,        musicVolume, t);
            fadeOut.volume = Mathf.Lerp(startVol,  0f,          t);
            yield return null;
        }

        fadeOut.Stop();
        fadeOut.volume = 0f;
        fadeIn.volume  = musicVolume;
    }

    private IEnumerator FadeOut(AudioSource src, float duration)
    {
        float start = src.volume;
        float elapsed = 0f;
        while (elapsed < duration) { elapsed += Time.deltaTime; src.volume = Mathf.Lerp(start, 0f, elapsed / duration); yield return null; }
        src.Stop();
    }

    private AudioSource ActiveMusic()   => _usingA ? _musicA : _musicB;
    private AudioSource InactiveMusic() => _usingA ? _musicB : _musicA;

    private AudioSource CreateSource(string label, float volume, bool loop)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.volume = volume;
        src.loop   = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f; // 2D
        return src;
    }
}
