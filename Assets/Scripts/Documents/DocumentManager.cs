using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Синглтон. Принимает документ, добавляет его в архив и запускает нужный UI.
///
/// Нарративные триггеры:
///   DocumentManager.OnDocumentRead += (doc) => { if (doc.id == "...") ... };
///
/// Выживает между сценами (DontDestroyOnLoad).
/// </summary>
public class DocumentManager : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static DocumentManager Instance { get; private set; }

    // ── Events ─────────────────────────────────────────────────────────────
    /// <summary>Вызывается при первом прочтении документа. Используй для нарративных триггеров.</summary>
    public static event Action<DocumentData> OnDocumentRead;

    // ── State ──────────────────────────────────────────────────────────────
    private readonly HashSet<string>   _foundIds = new HashSet<string>();
    private readonly List<DocumentData> _archive  = new List<DocumentData>();

    // ── Inspector ──────────────────────────────────────────────────────────
    [SerializeField] private DocumentReaderUI readerUI;

    [Header("Audio cassette playback")]
    [SerializeField] private AudioSource cassetteSource;

    [Header("Cassette indicator (optional)")]
    [SerializeField] private GameObject cassettePlayingIndicator;

    private Coroutine _cassetteRoutine;

    // ── Unity lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Создаём AudioSource для кассет, если не назначен
        if (cassetteSource == null)
        {
            cassetteSource              = gameObject.AddComponent<AudioSource>();
            cassetteSource.spatialBlend = 0f;   // 2D — слышно везде
            cassetteSource.playOnAwake  = false;
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Открывает документ: добавляет в архив и показывает нужный UI.
    /// Можно вызывать повторно — повторное чтение не добавляет документ дважды.
    /// </summary>
    public void Read(DocumentData doc)
    {
        if (doc == null) return;

        bool isNew = _foundIds.Add(doc.id);
        if (isNew)
        {
            _archive.Add(doc);
            _archive.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
        }

        if (doc.type == DocumentData.DocumentType.Audio)
        {
            PlayCassette(doc);
        }
        else
        {
            if (readerUI != null)
                readerUI.Show(doc, playTypewriter: isNew);
            else
                Debug.LogWarning("[DocumentManager] readerUI не назначен!", this);
        }

        if (isNew)
            OnDocumentRead?.Invoke(doc);
    }

    /// <summary>Возвращает true, если документ с этим id уже был найден.</summary>
    public bool IsFound(string id) => _foundIds.Contains(id);

    /// <summary>Отсортированный список всех найденных документов для архива в меню паузы.</summary>
    public IReadOnlyList<DocumentData> GetArchive() => _archive;

    // ── Save system support ────────────────────────────────────────────────

    /// <summary>
    /// Добавить документ в архив без показа UI.
    /// Вызывается DocumentWorldObject.RestoreState() при загрузке сохранения.
    /// </summary>
    public void AddToArchiveQuiet(DocumentData doc)
    {
        if (doc == null) return;
        if (_foundIds.Add(doc.id))
        {
            _archive.Add(doc);
            _archive.Sort((a, b) => string.Compare(a.id, b.id, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Сбросить архив перед применением данных загрузки.
    /// Вызывается SaveManager в начале ApplySaveFile().
    /// </summary>
    public void ResetForLoad()
    {
        _foundIds.Clear();
        _archive.Clear();
        if (cassetteSource != null && cassetteSource.isPlaying) cassetteSource.Stop();
        if (_cassetteRoutine != null) { StopCoroutine(_cassetteRoutine); _cassetteRoutine = null; }
        if (cassettePlayingIndicator != null) cassettePlayingIndicator.SetActive(false);
    }

    // ── Cassette playback ──────────────────────────────────────────────────

    private void PlayCassette(DocumentData doc)
    {
        if (doc.audioClip == null) return;

        if (_cassetteRoutine != null) StopCoroutine(_cassetteRoutine);
        _cassetteRoutine = StartCoroutine(CassetteRoutine(doc.audioClip));
    }

    private IEnumerator CassetteRoutine(AudioClip clip)
    {
        cassetteSource.Stop();
        cassetteSource.clip = clip;
        cassetteSource.Play();

        if (cassettePlayingIndicator != null)
            cassettePlayingIndicator.SetActive(true);

        // Ждём до конца записи
        yield return new WaitForSeconds(clip.length);

        if (cassettePlayingIndicator != null)
            cassettePlayingIndicator.SetActive(false);
    }
}
