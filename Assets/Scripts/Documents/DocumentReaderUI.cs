using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полноэкранный оверлей для чтения документов (Paper / MultiPart).
///
/// Иерархия Unity UI (минимальная):
///   DocumentReaderPanel (CanvasGroup — поле "overlay")
///     └─ Panel (поле "panel")
///         ├─ TitleText  (TMP)
///         ├─ Divider    (Image — горизонтальная черта)
///         └─ ScrollArea (ScrollRect)
///             └─ Viewport/Content
///                 └─ BodyText (TMP — поле "bodyText")
///
/// Управление:
///   E — пропустить анимацию → закрыть.
///   Колесо/стрелки — прокрутка (нативный ScrollRect).
/// </summary>
public class DocumentReaderUI : MonoBehaviour
{
    [Header("Canvas группа оверлея")]
    [SerializeField] private CanvasGroup overlay;

    [Header("Корень панели")]
    [SerializeField] private GameObject panel;

    [Header("Текстовые поля")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;

    [Header("Скролл (опционально)")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Анимация печатной машинки")]
    [SerializeField] private float charsPerSecond = 30f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.25f;

    [Header("Ссылки")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Фоновый звук при чтении (опционально)")]
    [SerializeField] private AudioSource ambientSource;

    // ── Runtime state ──────────────────────────────────────────────────────
    private Coroutine _typeRoutine;
    private Coroutine _fadeRoutine;
    private DocumentData _current;
    private bool _typingDone;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (inputHandler == null)
            inputHandler = FindObjectOfType<PlayerInputHandler>();

        if (overlay != null)
        {
            overlay.alpha          = 0f;
            overlay.interactable   = false;
            overlay.blocksRaycasts = false;
        }
        if (panel != null) panel.SetActive(false);
    }

    private void Update()
    {
        if (_current == null) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!_typingDone)
                SkipTyping();
            else
                Hide();
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Открыть оверлей с документом.
    /// playTypewriter=true при первом чтении, false при повторном (текст сразу).
    /// </summary>
    public void Show(DocumentData doc, bool playTypewriter = true)
    {
        if (doc == null) return;
        _current = doc;

        if (titleText != null) titleText.text = doc.title;
        if (bodyText  != null) bodyText.text  = string.Empty;

        if (panel != null) panel.SetActive(true);

        // Заблокировать ввод игрока
        if (inputHandler != null) inputHandler.InputEnabled = false;

        // Фоновый звук
        if (doc.ambientOnRead != null && ambientSource != null)
        {
            ambientSource.clip = doc.ambientOnRead;
            ambientSource.loop = true;
            ambientSource.Play();
        }

        // Подготовить оверлей к fade-in
        if (overlay != null)
        {
            overlay.interactable   = true;
            overlay.blocksRaycasts = true;
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeTo(1f));

        // Запустить или сразу вывести текст
        _typingDone = !playTypewriter;
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);

        if (playTypewriter)
            _typeRoutine = StartCoroutine(TypewriterRoutine(doc.body));
        else
            if (bodyText != null) bodyText.text = doc.body;

        // Прокрутить вверх
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void Hide()
    {
        _current = null;

        if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }

        // Остановить фоновый звук
        if (ambientSource != null && ambientSource.isPlaying) ambientSource.Stop();

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutAndHide());

        // Вернуть управление
        if (inputHandler != null) inputHandler.InputEnabled = true;
    }

    // ── Internals ──────────────────────────────────────────────────────────

    /// <summary>Мгновенно вывести весь текст, не ждать анимации.</summary>
    private void SkipTyping()
    {
        if (_typeRoutine != null) { StopCoroutine(_typeRoutine); _typeRoutine = null; }
        if (bodyText != null && _current != null) bodyText.text = _current.body;
        _typingDone = true;
    }

    private IEnumerator TypewriterRoutine(string fullText)
    {
        _typingDone = false;
        if (bodyText == null) { _typingDone = true; yield break; }

        float delay = 1f / Mathf.Max(charsPerSecond, 1f);

        for (int i = 0; i <= fullText.Length; i++)
        {
            bodyText.text = fullText.Substring(0, i);

            // Прокручивать вниз по мере появления текста
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;

            yield return new WaitForSeconds(delay);
        }
        _typingDone = true;
    }

    private IEnumerator FadeTo(float target)
    {
        if (overlay == null) yield break;
        float start   = overlay.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed    += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        overlay.alpha = target;
    }

    private IEnumerator FadeOutAndHide()
    {
        if (overlay != null)
        {
            overlay.interactable   = false;
            overlay.blocksRaycasts = false;
            yield return StartCoroutine(FadeTo(0f));
        }
        if (panel != null) panel.SetActive(false);
    }
}
