using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Финальный объект — Кристалл.
/// Реализует IInteractable: игрок подходит, жмёт E, появляется выбор двух концовок.
///
/// Иерархия объекта в сцене:
///   Crystal (этот компонент + SaveableIdentity + Collider на слое Interactable)
///     └─ CrystalGlow  (ParticleSystem или Light, мигает)
///
/// Иерархия UI (отдельно на Canvas):
///   CrystalChoicePanel  CanvasGroup (= choicePanel)
///     ├─ TitleText       TMP
///     ├─ DescriptionText TMP
///     ├─ DreamButton     Button
///     └─ SacrificeButton Button
///
/// Назначь ссылки в инспекторе.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CrystalEncounter : MonoBehaviour, IInteractable, ISaveable
{
    [Header("Ссылки на UI")]
    [SerializeField] private CanvasGroup     choicePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button          dreamButton;
    [SerializeField] private Button          sacrificeButton;

    [Header("Тексты")]
    [SerializeField] private string panelTitle = "К Р И С Т А Л Л";
    [SerializeField, TextArea(2,6)]
    private string panelDescription =
        "Ты чувствуешь пульс под ладонью.\n" +
        "Он живой. Он ждал тебя.\n\n" +
        "Что ты сделаешь?";

    [SerializeField] private string dreamButtonLabel     = "Слиться с Кристаллом";
    [SerializeField] private string sacrificeButtonLabel = "Уничтожить его";

    [Header("Звук и VFX")]
    [SerializeField] private AudioClip     ambientHum;      // тихий гул рядом с кристаллом
    [SerializeField] private AudioClip     touchSound;      // при взаимодействии
    [SerializeField] private ParticleSystem glowParticles;  // свечение

    [Header("Параметры")]
    [SerializeField] private float choiceFadeDuration = 0.6f;
    [SerializeField] private bool  alreadyUsed;              // если выбор уже сделан

    private AudioSource _audioSource;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1f;
        _audioSource.loop         = true;
        _audioSource.playOnAwake  = false;

        // Кнопки
        dreamButton    ?.onClick.AddListener(() => MakeChoice(GameEnding.EndingType.Dream));
        sacrificeButton?.onClick.AddListener(() => MakeChoice(GameEnding.EndingType.Sacrifice));

        if (choicePanel != null) { choicePanel.alpha = 0f; choicePanel.blocksRaycasts = false; choicePanel.interactable = false; }
    }

    private void Start()
    {
        // Фоновый гул Кристалла
        if (ambientHum != null)
        {
            _audioSource.clip   = ambientHum;
            _audioSource.volume = 0f;
            _audioSource.Play();
            StartCoroutine(FadeAudio(_audioSource, 0f, 0.4f, 3f));
        }

        if (glowParticles != null) glowParticles.Play();

        // Заполнить тексты
        if (titleText       != null) titleText.text       = panelTitle;
        if (descriptionText != null) descriptionText.text = panelDescription;
        if (dreamButton     != null) dreamButton    .GetComponentInChildren<TextMeshProUGUI>()?.SetText(dreamButtonLabel);
        if (sacrificeButton != null) sacrificeButton.GetComponentInChildren<TextMeshProUGUI>()?.SetText(sacrificeButtonLabel);
    }

    // ── IInteractable ──────────────────────────────────────────────────────

    public string PromptText => alreadyUsed ? null : "Прикоснуться";

    public void Interact()
    {
        if (alreadyUsed) return;
        alreadyUsed = true;

        if (touchSound != null) _audioSource.PlayOneShot(touchSound);

        // Заблокировать движение
        var input = FindObjectOfType<PlayerInputHandler>();
        if (input != null) input.InputEnabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        StartCoroutine(ShowChoice());
    }

    // ── Choice flow ────────────────────────────────────────────────────────

    private IEnumerator ShowChoice()
    {
        if (choicePanel == null) yield break;

        choicePanel.interactable   = true;
        choicePanel.blocksRaycasts = true;

        float elapsed = 0f;
        while (elapsed < choiceFadeDuration)
        {
            elapsed          += Time.deltaTime;
            choicePanel.alpha = Mathf.Lerp(0f, 1f, elapsed / choiceFadeDuration);
            yield return null;
        }
        choicePanel.alpha = 1f;
    }

    private void MakeChoice(GameEnding.EndingType ending)
    {
        // Скрыть панель
        if (choicePanel != null) { choicePanel.alpha = 0f; choicePanel.blocksRaycasts = false; choicePanel.interactable = false; }

        // Запустить концовку
        if (GameEnding.Instance != null)
        {
            GameEnding.Instance.Trigger(ending);
        }
        else
        {
            Debug.LogWarning("[CrystalEncounter] GameEnding.Instance не найден! Добавь GameEnding на Canvas.");
            // Fallback: перезапуск
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }

    // ── ISaveable ─────────────────────────────────────────────────────────

    [System.Serializable]
    private struct CrystalSaveData { public bool alreadyUsed; }

    public string CaptureState() =>
        JsonUtility.ToJson(new CrystalSaveData { alreadyUsed = this.alreadyUsed });

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<CrystalSaveData>(json);
        alreadyUsed = d.alreadyUsed;
        if (alreadyUsed)
        {
            // Кристалл уже использован — гасим частицы, отключаем гул
            if (glowParticles != null) glowParticles.Stop();
            if (_audioSource != null)  _audioSource.Stop();
        }
    }

    // ── Audio fade ─────────────────────────────────────────────────────────

    private static IEnumerator FadeAudio(AudioSource src, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            src.volume  = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        src.volume = to;
    }
}
