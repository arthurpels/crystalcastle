using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Экран смерти игрока.
///
/// Иерархия Canvas (поверх всего):
///   GameOverRoot  ←  CanvasGroup (= overlay)
///     ├─ Background     Image (чёрный, alpha 0 → 1)
///     ├─ DeathTitle     TMP  "В Ы  З А М Ё Р З Л И"
///     ├─ Subtitle        TMP  (опционально: цитата из лора)
///     ├─ RetryButton     Button
///     └─ MenuButton      Button
///
/// Показывается автоматически — PlayerAttributes вызывает Show() при смерти.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup     overlay;
    [SerializeField] private GameObject      panel;
    [SerializeField] private TextMeshProUGUI deathTitle;
    [SerializeField] private TextMeshProUGUI subtitle;
    [SerializeField] private Button          retryButton;
    [SerializeField] private Button          menuButton;

    [Header("Тексты")]
    [SerializeField] private string   deathText = "В Ы  З А М Ё Р З Л И";
    [SerializeField] private string[] subtitles = {
        "Холод всегда побеждает.",
        "Лаборатория не прощает слабых.",
        "Кристалл помнит всё.",
        "−58°C. Ты не почувствовал боли."
    };

    [Header("Тайминги")]
    [SerializeField] private float blackoutDelay = 0.8f;
    [SerializeField] private float fadeDuration  = 1.8f;
    [SerializeField] private float titleDelay    = 0.5f;

    [Header("Звук")]
    [SerializeField] private AudioClip deathStinger;

    [Header("Сцены")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (menuButton  != null) menuButton.onClick.AddListener(GoToMenu);

        if (overlay != null) { overlay.alpha = 0f; overlay.interactable = false; overlay.blocksRaycasts = false; }
        if (panel   != null) panel.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
        StartCoroutine(ShowSequence());
    }

    // ── Internals ─────────────────────────────────────────────────────────

    private IEnumerator ShowSequence()
    {
        yield return new WaitForSecondsRealtime(blackoutDelay);

        if (panel != null) panel.SetActive(true);

        // Скрыть кнопки пока не появится текст
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (menuButton  != null) menuButton.gameObject.SetActive(false);
        if (deathTitle  != null) deathTitle.alpha = 0f;
        if (subtitle    != null) subtitle.alpha   = 0f;

        // Разблокировать overlay
        if (overlay != null) { overlay.interactable = true; overlay.blocksRaycasts = true; }

        // Fade-in чёрного фона
        yield return StartCoroutine(FadeOverlay(0f, 1f, fadeDuration));

        // Звук смерти
        if (deathStinger != null)
            AudioSource.PlayClipAtPoint(deathStinger, Vector3.zero);

        yield return new WaitForSecondsRealtime(titleDelay);

        // Показать заголовок
        if (deathTitle != null)
        {
            deathTitle.text = deathText;
            yield return StartCoroutine(FadeText(deathTitle, 0f, 1f, 0.8f));
        }

        yield return new WaitForSecondsRealtime(0.4f);

        // Подзаголовок (рандомная цитата)
        if (subtitle != null && subtitles.Length > 0)
        {
            subtitle.text = subtitles[Random.Range(0, subtitles.Length)];
            yield return StartCoroutine(FadeText(subtitle, 0f, 1f, 0.6f));
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // Показать кнопки
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (menuButton  != null) menuButton.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Fade helpers ──────────────────────────────────────────────────────

    private IEnumerator FadeOverlay(float from, float to, float duration)
    {
        if (overlay == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        overlay.alpha = to;
    }

    private static IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            text.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        text.alpha = to;
    }
}
