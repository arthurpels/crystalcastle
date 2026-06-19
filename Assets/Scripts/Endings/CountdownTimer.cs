using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Нода-задержка (детонатор): когда ВХОДНАЯ нода под током — стартует отсчёт;
/// по истечению времени запитывает ВЫХОДНУЮ ноду (сигнал течёт дальше по сети,
/// например на детонатор-IPowerable). Параллельно выводит цифры, включает
/// сирену и мигалки.
///
/// Реализует IPowerGate (собирается PowerNetwork). Compute = «истёк ли таймер».
/// </summary>
public class CountdownTimer : MonoBehaviour, IPowerGate
{
    [Header("Сеть")]
    [Tooltip("Входная нода: как только под током — стартует отсчёт (взвод).")]
    [SerializeField] private PowerNode startNode;
    [Tooltip("Выходная нода: получает питание, когда таймер истёк.")]
    [SerializeField] private PowerNode output;

    [Header("Таймер")]
    [Tooltip("Длительность отсчёта, сек.")]
    [SerializeField] private float duration = 90f;
    [Tooltip("Требовать питание на входе всё время: пропало — отсчёт стоп+сброс. " +
             "Выкл — раз пошёл, идёт до нуля независимо от входа.")]
    [SerializeField] private bool requireSustainedPower = true;

    [Header("Дисплей")]
    [Tooltip("TMP-текст (3D или UGUI) для цифр MM:SS. Цвет задаёшь на самом тексте.")]
    [SerializeField] private TMP_Text display;

    [Header("Тревога")]
    [SerializeField] private AudioSource siren;
    [SerializeField] private GameObject[] warningLights;

    [Header("События (опц.)")]
    public UnityEvent onStarted;
    public UnityEvent onElapsed;
    public UnityEvent onCancelled;   // питание пропало до нуля

    // ── IPowerGate ───────────────────────────────────────────────────────────
    public PowerNode Output => output;
    public bool Compute(HashSet<PowerNode> powered) => _elapsed;

    public bool IsRunning => _running;

    private bool  _running;
    private bool  _elapsed;
    private float _timeLeft;

    private void Update()
    {
        // Старт по входной ноде (опрос, как у SignalChecker — без побочек в Compute).
        if (!_running && !_elapsed && startNode != null && startNode.IsPowered)
            StartCountdown();

        if (!_running) return;

        // Питание на входе пропало → стоп и сброс.
        if (requireSustainedPower && startNode != null && !startNode.IsPowered)
        {
            CancelCountdown();
            return;
        }

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            UpdateDisplay();
            Elapse();
            return;
        }
        UpdateDisplay();
    }

    /// <summary>Запустить отсчёт вручную (из UnityEvent терминала и т.п.).</summary>
    public void StartCountdown()
    {
        if (_running || _elapsed) return;
        _running  = true;
        _timeLeft = duration;

        if (siren != null) siren.Play();
        SetWarningLights(true);
        UpdateDisplay();
        onStarted?.Invoke();
    }

    /// <summary>Остановить и сбросить отсчёт (питание пропало). Сигнал не выдаётся.</summary>
    public void CancelCountdown()
    {
        if (!_running) return;
        _running  = false;
        _timeLeft = duration;

        if (siren != null) siren.Stop();
        SetWarningLights(false);
        UpdateDisplay();          // покажет полное время (готов)
        onCancelled?.Invoke();
    }

    private void Elapse()
    {
        _running = false;
        _elapsed = true;
        if (siren != null) siren.Stop();

        // Проталкиваем сигнал на выходную ноду (Compute теперь вернёт true).
        PowerNetwork.Instance?.Evaluate();
        onElapsed?.Invoke();
    }

    private void UpdateDisplay()
    {
        if (display == null) return;

        int t = Mathf.CeilToInt(_timeLeft);
        display.text = $"{t / 60:00}:{t % 60:00}";   // цвет не трогаем — задаёшь на тексте
    }

    private void SetWarningLights(bool on)
    {
        if (warningLights == null) return;
        foreach (var w in warningLights)
            if (w != null) w.SetActive(on);
    }
}
