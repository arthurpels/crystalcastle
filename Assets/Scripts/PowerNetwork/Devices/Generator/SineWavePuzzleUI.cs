using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SineWavePuzzleUI : MonoBehaviour
{
    [System.Serializable]
    public class WaveParams
    {
        public float amplitude = 1f;
        public float frequency = 1f;
        public float phase = 0f;
    }

    [Header("Settings")]
    [SerializeField] private WaveParams targetWave;
    [SerializeField] private WaveParams playerWave;
    [SerializeField] private float tolerance = 0.15f;
    [SerializeField] private int resolution = 128;
    [SerializeField] private float graphWidth = 400f;
    [SerializeField] private float graphHeight = 100f;

    [Header("UI")]
    [SerializeField] private Slider ampSlider;    // range 0.5 – 2.0
    [SerializeField] private Slider freqSlider;   // range 0.5 – 3.0
    [SerializeField] private Slider phaseSlider;  // range 0 – 6.28
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button exitButton;            // НОВОЕ — кнопка выхода (опционально)
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private RectTransform shakeTarget;    // НОВОЕ — панель для дрожания при ошибке (опц.)
    [SerializeField] private UILineRenderer targetLine;
    [SerializeField] private UILineRenderer playerLine;

    [Header("Обратная связь")]
    [SerializeField] private Color normalColor  = new Color(0.82f, 0.86f, 0.90f);
    [SerializeField] private Color successColor = new Color(0.45f, 1.00f, 0.55f);
    [SerializeField] private Color errorColor   = new Color(1.00f, 0.38f, 0.32f);
    [SerializeField] private float errorHoldTime = 1.6f;   // сколько висит текст ошибки
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;

    [Header("Refs")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private CursorController cursorController;

    private System.Action _onComplete;
    private Coroutine _shakeRoutine;
    private Coroutine _statusRoutine;
    private Coroutine _finishRoutine;
    private Vector2 _shakeHome;

    void Awake()
    {
        gameObject.SetActive(false);
        confirmButton?.onClick.AddListener(OnConfirm);
        exitButton?.onClick.AddListener(Cancel);

        ampSlider?.onValueChanged.AddListener(v => { playerWave.amplitude = v; UpdateGraph(); });
        freqSlider?.onValueChanged.AddListener(v => { playerWave.frequency = v; UpdateGraph(); });
        phaseSlider?.onValueChanged.AddListener(v => { playerWave.phase = v; UpdateGraph(); });

        if (shakeTarget != null) _shakeHome = shakeTarget.anchoredPosition;
    }

    public void StartPuzzle(System.Action onComplete)
    {
        inputHandler.SetInputEnabled(false);
        cursorController.UnlockForUI();

        _onComplete = onComplete;
        gameObject.SetActive(true);

        if (confirmButton != null) confirmButton.interactable = true;

        // Генерируем целевую волну
        targetWave.amplitude = Random.Range(0.8f, 2.0f);
        targetWave.frequency = Random.Range(0.5f, 2.5f);
        targetWave.phase     = Random.Range(0f, Mathf.PI * 2f);

        // Сброс игрока
        ampSlider.value = 1.0f;
        freqSlider.value = 1.0f;
        phaseSlider.value = 0f;
        playerWave.amplitude = 1f;
        playerWave.frequency = 1f;
        playerWave.phase = 0f;

        SetStatus("Настройте параметры сигнала...", normalColor);
        UpdateGraph();
    }

    void UpdateGraph()
    {
        targetLine.SetPoints(GeneratePoints(targetWave));
        playerLine.SetPoints(GeneratePoints(playerWave));
    }

    Vector2[] GeneratePoints(WaveParams p)
    {
        Vector2[] pts = new Vector2[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            float x = t * graphWidth;
            float y = p.amplitude * Mathf.Sin(p.frequency * t * Mathf.PI * 2 + p.phase) * (graphHeight * 0.5f);
            pts[i] = new Vector2(x, y);
        }
        return pts;
    }

    void OnConfirm()
    {
        if (_finishRoutine != null) return; // уже синхронизировано, ждём закрытия

        if (CheckMatch())
        {
            if (confirmButton != null) confirmButton.interactable = false;
            SetStatus("СИГНАЛ СИНХРОНИЗИРОВАН", successColor);
            if (successSound != null) AudioManager.PlaySFX(successSound);
            _finishRoutine = StartCoroutine(FinishAfter(0.6f));
        }
        else
        {
            SetStatus("РАССОГЛАСОВАНИЕ — ПРОДОЛЖАЙТЕ НАСТРОЙКУ", errorColor);
            if (errorSound != null) AudioManager.PlaySFX(errorSound);

            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(Shake());

            if (_statusRoutine != null) StopCoroutine(_statusRoutine);
            _statusRoutine = StartCoroutine(ResetStatusAfter(errorHoldTime));
        }
    }

    bool CheckMatch()
    {
        bool amp  = Mathf.Abs(playerWave.amplitude - targetWave.amplitude) < tolerance;
        bool freq = Mathf.Abs(playerWave.frequency - targetWave.frequency) < tolerance;

        float phDiff = Mathf.Abs(playerWave.phase - targetWave.phase);
        phDiff = Mathf.Min(phDiff, Mathf.PI * 2 - phDiff);
        bool ph = phDiff < tolerance;

        return amp && freq && ph;
    }

    /// <summary>Успешное завершение — включает генератор.</summary>
    private IEnumerator FinishAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (shakeTarget != null) shakeTarget.anchoredPosition = _shakeHome;
        RestoreControl();
        _onComplete?.Invoke();
    }

    /// <summary>Выход без решения (ESC или кнопка). Генератор остаётся выключенным.</summary>
    public void Cancel()
    {
        StopAllCoroutines();
        _shakeRoutine = _statusRoutine = _finishRoutine = null;
        if (shakeTarget != null) shakeTarget.anchoredPosition = _shakeHome;
        RestoreControl();
        // _onComplete НЕ вызываем
    }

    private void RestoreControl()
    {
        gameObject.SetActive(false);
        inputHandler.SetInputEnabled(true);
        cursorController.LockForGameplay();
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private IEnumerator ResetStatusAfter(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        SetStatus("Настройте параметры сигнала...", normalColor);
        _statusRoutine = null;
    }

    private IEnumerator Shake()
    {
        if (shakeTarget == null) yield break;

        const float dur = 0.35f;
        const float mag = 12f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float damper = 1f - (t / dur);
            float x = (Random.value * 2f - 1f) * mag * damper;
            float y = (Random.value * 2f - 1f) * mag * damper;
            shakeTarget.anchoredPosition = _shakeHome + new Vector2(x, y);
            yield return null;
        }
        shakeTarget.anchoredPosition = _shakeHome;
        _shakeRoutine = null;
    }
}
