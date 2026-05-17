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
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private UILineRenderer targetLine;
    [SerializeField] private UILineRenderer playerLine;

    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private CursorController cursorController;

    private System.Action _onComplete;

    void Awake()
    {
        gameObject.SetActive(false);
        confirmButton?.onClick.AddListener(OnConfirm);

        ampSlider?.onValueChanged.AddListener(v => { playerWave.amplitude = v; UpdateGraph(); });
        freqSlider?.onValueChanged.AddListener(v => { playerWave.frequency = v; UpdateGraph(); });
        phaseSlider?.onValueChanged.AddListener(v => { playerWave.phase = v; UpdateGraph(); });
    }

    public void StartPuzzle(System.Action onComplete)
    {
        inputHandler.SetInputEnabled(false);
        cursorController.UnlockForUI();

            


        _onComplete = onComplete;
        gameObject.SetActive(true);

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

        statusText.text = "Настройте параметры сигнала...";
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
        if (CheckMatch())
        {
            statusText.text = "СИГНАЛ СИНХРОНИЗИРОВАН";
            Invoke(nameof(Finish), 0.5f);
        }
        else
        {
            statusText.text = "ОШИБКА: несоответствие параметров";
            // TODO: звук ошибки, дрожание UI
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

    void Finish()
    {
        gameObject.SetActive(false);

        inputHandler.SetInputEnabled(true);
        cursorController.LockForGameplay();
        
        _onComplete?.Invoke();
    }
}
