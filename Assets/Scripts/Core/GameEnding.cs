using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Система финальных концовок.
///
/// === Сон (Dream) ===
/// Игрок сливается с Кристаллом. Угасание в белом.
/// Кристалл принял нового хозяина. Программа "Мороз" продолжается.
///
/// === Жертва (Sacrifice) ===
/// Игрок уничтожает Кристалл. Взрыв. Угасание в чёрном.
/// Кристалл мёртв. Лаборатория — навсегда.
///
/// Иерархия:
///   GameEndingRoot  CanvasGroup (= overlay)
///     ├─ Background   Image (white или black)
///     ├─ EndingTitle  TMP  (крупный заголовок)
///     ├─ EndingBody   TMP  (текст концовки, typewriter)
///     └─ MenuButton   Button "Главное меню"
///
/// Вызов: GameEnding.Instance.Trigger(EndingType.Dream);
/// </summary>
public class GameEnding : MonoBehaviour
{
    public enum EndingType { Dream, Sacrifice }

    public static GameEnding Instance { get; private set; }

    // ── Данные концовок ────────────────────────────────────────────────────

    [System.Serializable]
    public struct EndingData
    {
        public string title;
        [TextArea(3, 10)]
        public string body;
        public Color  backgroundColor;
        public Color  textColor;
        public AudioClip music;
    }

    [Header("Концовки")]
    [SerializeField] private EndingData dreamEnding = new EndingData
    {
        title           = "С О Н",
        body            =
            "Ты слышишь его.\n\n" +
            "Не словами — кристаллами.\n" +
            "Не смыслом — частотой.\n\n" +
            "Холод стал теплом.\n" +
            "Тьма стала светом.\n\n" +
            "Ты понял всё.\n\n" +
            "Ты остался.",
        backgroundColor = Color.white,
        textColor       = new Color(0.08f, 0.08f, 0.08f)
    };

    [SerializeField] private EndingData sacrificeEnding = new EndingData
    {
        title           = "Ж Е Р Т В А",
        body            =
            "Взрыв.\n\n" +
            "Тишина.\n\n" +
            "Свет уходит последним.\n\n" +
            "Ты не вернулся.\n\n" +
            "Но Кристалл мёртв.\n\n" +
            "Программа \"Мороз\" закрыта.\n" +
            "Навсегда.",
        backgroundColor = Color.black,
        textColor       = new Color(0.85f, 0.85f, 0.85f)
    };

    // ── UI ────────────────────────────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private CanvasGroup     overlay;
    [SerializeField] private Image           background;
    [SerializeField] private TextMeshProUGUI endingTitle;
    [SerializeField] private TextMeshProUGUI endingBody;
    [SerializeField] private Button          menuButton;

    [Header("Тайминги")]
    [SerializeField] private float preFadePause   = 1.0f;
    [SerializeField] private float fadeDuration   = 2.5f;
    [SerializeField] private float titleDelay     = 1.0f;
    [SerializeField] private float charsPerSecond = 18f;
    [SerializeField] private float postTextDelay  = 1.5f;

    [Header("Сцены")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (menuButton != null) menuButton.onClick.AddListener(GoToMenu);
        if (menuButton != null) menuButton.gameObject.SetActive(false);

        if (overlay != null) { overlay.alpha = 0f; overlay.blocksRaycasts = false; }
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Trigger(EndingType type)
    {
        var data = type == EndingType.Dream ? dreamEnding : sacrificeEnding;
        StartCoroutine(PlayEnding(data, type));
    }

    // ── Ending sequence ───────────────────────────────────────────────────

    private IEnumerator PlayEnding(EndingData data, EndingType type)
    {
        // Заблокировать ввод
        var input = FindObjectOfType<PlayerInputHandler>();
        if (input != null) input.InputEnabled = false;

        // Встряска камеры для Sacrifice
        if (type == EndingType.Sacrifice)
        {
            if (CameraShake.Instance != null)
                CameraShake.Instance.Shake(0.25f, 0.8f);
        }

        yield return new WaitForSecondsRealtime(preFadePause);

        // Настроить цвета
        if (background  != null) background.color = data.backgroundColor;
        if (endingTitle != null) { endingTitle.color = data.textColor; endingTitle.alpha = 0f; }
        if (endingBody  != null) { endingBody.color  = data.textColor; endingBody.alpha  = 0f; endingBody.text = string.Empty; }

        // Активировать overlay
        if (overlay != null) { overlay.blocksRaycasts = true; overlay.interactable = true; }

        // Сменить музыку
        if (data.music != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTrack(data.music);
        }
        else
        {
            if (AudioManager.Instance != null) AudioManager.Instance.StopMusic();
        }

        // Fade
        yield return StartCoroutine(FadeGroup(overlay, 0f, 1f, fadeDuration));

        yield return new WaitForSecondsRealtime(titleDelay);

        // Заголовок
        if (endingTitle != null)
        {
            endingTitle.text = data.title;
            yield return StartCoroutine(FadeText(endingTitle, 0f, 1f, 0.8f));
        }

        yield return new WaitForSecondsRealtime(0.6f);

        // Typewriter для текста концовки
        if (endingBody != null)
        {
            endingBody.alpha = 1f;
            yield return StartCoroutine(Typewriter(endingBody, data.body, charsPerSecond));
        }

        yield return new WaitForSecondsRealtime(postTextDelay);

        // Показать кнопку
        if (menuButton != null) menuButton.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    // ── Button ────────────────────────────────────────────────────────────

    private void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Корутины ─────────────────────────────────────────────────────────

    private static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur) { e += Time.unscaledDeltaTime; g.alpha = Mathf.Lerp(from, to, e / dur); yield return null; }
        g.alpha = to;
    }

    private static IEnumerator FadeText(TextMeshProUGUI t, float from, float to, float dur)
    {
        float e = 0f;
        while (e < dur) { e += Time.unscaledDeltaTime; t.alpha = Mathf.Lerp(from, to, e / dur); yield return null; }
        t.alpha = to;
    }

    private static IEnumerator Typewriter(TextMeshProUGUI t, string text, float cps)
    {
        float delay = 1f / Mathf.Max(cps, 1f);
        for (int i = 0; i <= text.Length; i++)
        {
            t.text = text.Substring(0, i);
            yield return new WaitForSecondsRealtime(delay);
        }
    }
}
