using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Меню паузы. Открывается по Escape.
/// Интегрирован с SaveSlotUI для сохранения/загрузки.
///
/// Иерархия:
///   PauseMenuPanel (этот компонент)
///     ├─ ResumeButton
///     ├─ SaveButton
///     ├─ LoadButton
///     ├─ QuitButton
///     └─ StatusText (TMP, опционально — "Игра сохранена")
///
/// Также добавь SaveSlotUI где-нибудь на Canvas — PauseMenu вызывает его.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("Панель")]
    [SerializeField] private GameObject panel;

    [Header("Кнопки")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button quitButton;

    [Header("Статус (опционально)")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float           statusShowDuration = 2f;

    [Header("Основное меню")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Ссылки")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private SaveSlotUI         saveSlotUI;

    private bool _isPaused;
    private float _statusTimer;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        resumeButton?.onClick.AddListener(Resume);
        saveButton  ?.onClick.AddListener(OpenSaveSlots);
        loadButton  ?.onClick.AddListener(OpenLoadSlots);
        quitButton  ?.onClick.AddListener(QuitToMenu);

        if (panel != null) panel.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);

        if (inputHandler == null)
            inputHandler = FindObjectOfType<PlayerInputHandler>();

        SaveManager.OnAfterLoad += OnAfterLoad;
    }

    private void OnDestroy() => SaveManager.OnAfterLoad -= OnAfterLoad;

    private void Update()
    {
        // Escape открывает/закрывает паузу (но не если открыт SaveSlotUI)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (saveSlotUI != null && saveSlotUI.gameObject.activeSelf)
            {
                saveSlotUI.Close();
                return;
            }
            Toggle();
        }

        // Скрываем статусный текст по таймеру
        if (_statusTimer > 0f)
        {
            _statusTimer -= Time.unscaledDeltaTime;
            if (_statusTimer <= 0f && statusText != null)
                statusText.gameObject.SetActive(false);
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────

    public void Toggle() { if (_isPaused) Resume(); else Pause(); }

    public void Pause()
    {
        _isPaused = true;
        Time.timeScale = 0f;
        if (panel != null) panel.SetActive(true);
        if (inputHandler != null) inputHandler.InputEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Resume()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (panel != null) panel.SetActive(false);
        if (saveSlotUI != null) saveSlotUI.Close();
        if (inputHandler != null) inputHandler.InputEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ── Buttons ────────────────────────────────────────────────────────────

    private void OpenSaveSlots()
    {
        if (saveSlotUI == null) { QuickSave(); return; }
        saveSlotUI.Open(SaveSlotUI.Mode.Save);
    }

    private void OpenLoadSlots()
    {
        if (saveSlotUI == null) return;
        saveSlotUI.Open(SaveSlotUI.Mode.Load);
    }

    private void QuickSave()
    {
        SaveManager.Instance?.Save(0);
        ShowStatus("Сохранено");
    }

    private void QuitToMenu()
    {
        Resume(); // вернуть timeScale = 1 перед сменой сцены
        SceneManager.LoadScene(mainMenuScene);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void ShowStatus(string text)
    {
        if (statusText == null) return;
        statusText.text = text;
        statusText.gameObject.SetActive(true);
        _statusTimer = statusShowDuration;
    }

    private void OnAfterLoad()
    {
        // Убеждаемся что пауза снята после загрузки
        Time.timeScale = 1f;
        _isPaused = false;
    }
}
