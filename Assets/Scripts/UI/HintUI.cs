using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Экранная подсказка с фейдом. Вызывается из NarrativeTrigger через UnityEvent:
///   ShowHint(string) — длительность по умолчанию (для статического аргумента в инспекторе).
///   Show(string, float) — явная длительность из кода.
/// </summary>
public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [SerializeField] private CanvasGroup     group;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float fadeDuration    = 0.3f;
    [SerializeField] private float defaultDuration = 4f;

    private Coroutine _routine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (group != null) group.alpha = 0f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Для UnityEvent в инспекторе (один строковый аргумент).</summary>
    public void ShowHint(string message) => Show(message, defaultDuration);

    public void Show(string message, float seconds)
    {
        if (text != null) text.text = message;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(ShowRoutine(seconds));
    }

    private IEnumerator ShowRoutine(float seconds)
    {
        yield return Fade(1f);
        yield return new WaitForSeconds(seconds);
        yield return Fade(0f);
        _routine = null;
    }

    private IEnumerator Fade(float target)
    {
        if (group == null) yield break;

        float start = group.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        group.alpha = target;
    }
}
