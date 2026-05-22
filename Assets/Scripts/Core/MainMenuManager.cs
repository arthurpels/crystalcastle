using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Главное меню. Добавь на любой GameObject в сцене "MainMenu".
///
/// Иерархия (минимальная):
///   Canvas
///     ├─ Title          TMP  "C R Y S T A L  C A S T L E"
///     ├─ Subtitle       TMP  "Чукотка, 1987"
///     ├─ NewGameButton  Button
///     ├─ LoadGameButton Button
///     ├─ QuitButton     Button
///     └─ VersionText    TMP  (опционально, v0.1a)
///
/// Музыка: назначь mainMenuMusic — AudioManager воспроизведёт её.
/// Добавь в Build Settings: [0] MainMenu, [1] SampleScene (или твоя игровая сцена).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Кнопки")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button quitButton;

    [Header("Сцены")]
    [SerializeField] private string gameScene = "SampleScene";

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private CanvasGroup     menuGroup;
    [SerializeField] private SaveSlotUI      saveSlotUI;

    [Header("Вступительный fade-in")]
    [SerializeField] private CanvasGroup blackScreen;
    [SerializeField] private float       introFadeDuration = 1.5f;

    [Header("Музыка")]
    [SerializeField] private AudioClip mainMenuMusic;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (newGameButton  != null) newGameButton.onClick.AddListener(StartNewGame);
        if (loadGameButton != null) loadGameButton.onClick.AddListener(OpenLoadMenu);
        if (quitButton     != null) quitButton.onClick.AddListener(QuitGame);

        RefreshLoadButton();

        SaveManager.OnAfterLoad += OnAfterLoad;

        if (mainMenuMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayTrack(mainMenuMusic);

        if (blackScreen != null)
        {
            blackScreen.alpha = 1f;
            blackScreen.blocksRaycasts = true;
            StartCoroutine(FadeInFromBlack());
        }
    }

    private void OnDestroy() {
        SaveManager.OnAfterLoad -= OnAfterLoad;
    }

    // ── Button handlers ────────────────────────────────────────────────────

    private void StartNewGame()
    {
        StartCoroutine(LoadScene(gameScene));
    }

    private void OpenLoadMenu()
    {
        if (saveSlotUI == null)
        {
            if (SaveManager.Instance != null && SaveManager.Instance.SlotExists(0))
                SaveManager.Instance.Load(0);
            return;
        }
        saveSlotUI.Open(SaveSlotUI.Mode.Load);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void RefreshLoadButton()
    {
        if (loadGameButton == null || SaveManager.Instance == null) return;
        bool anySave = false;
        for (int i = 0; i < SaveManager.SlotCount; i++)
        {
            if (SaveManager.Instance.SlotExists(i)) { anySave = true; break; }
        }
        loadGameButton.interactable = anySave;
    }

    private void OnAfterLoad() { }

    // ── Scene loading ──────────────────────────────────────────────────────

    private IEnumerator LoadScene(string sceneName)
    {
        if (blackScreen != null)
        {
            blackScreen.blocksRaycasts = true;
            yield return StartCoroutine(FadeGroup(blackScreen, 0f, 1f, 0.6f));
        }
        SceneManager.LoadScene(sceneName);
    }

    // ── Fade ──────────────────────────────────────────────────────────────

    private IEnumerator FadeInFromBlack()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        yield return StartCoroutine(FadeGroup(blackScreen, 1f, 0f, introFadeDuration));
        blackScreen.blocksRaycasts = false;
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
