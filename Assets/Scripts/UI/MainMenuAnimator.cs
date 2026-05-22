using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет анимацией появления главного меню Crystal Castle.
/// Добавь на тот же GameObject что и MainMenuManager.
///
/// ══ Иерархия Canvas (пример) ══════════════════════════════════════════
///   Canvas
///     ├─ BackgroundGroup          (Image — тёмный фон)
///     ├─ LogoGroup                (CanvasGroup ← поле logoGroup)
///     │    ├─ CrystalImage        (Image ← поле crystalImage)
///     │    ├─ TitleText           (TMP "CRYSTAL CASTLE" ← поле titleText)
///     │    ├─ SubtitleText        (TMP "ЧУКОТКА · 1987" ← поле subtitleText)
///     │    ├─ DecoLineLeft        (Image ← decoLines[0])
///     │    └─ DecoLineRight       (Image ← decoLines[1])
///     ├─ MenuGroup                (CanvasGroup ← поле menuGroup)
///     │    ├─ NewGameButton       (Button ← menuButtons[0])
///     │    ├─ LoadGameButton      (Button ← menuButtons[1])
///     │    └─ QuitButton          (Button ← menuButtons[2])
///     ├─ VersionText              (TMP, правый нижний угол)
///     ├─ BlackScreen              (CanvasGroup ← в MainMenuManager)
///     └─ SaveSlotUI               (для загрузки)
/// ══════════════════════════════════════════════════════════════════════
/// </summary>
[RequireComponent(typeof(MainMenuManager))]
public class MainMenuAnimator : MonoBehaviour
{
    // ── Логотип ───────────────────────────────────────────────────────────

    [Header("Логотип")]
    [SerializeField] private CanvasGroup     logoGroup;
    [SerializeField] private RectTransform   crystalRect;    // иконка кристалла
    [SerializeField] private TextMeshProUGUI titleText;      // "CRYSTAL CASTLE"
    [SerializeField] private TextMeshProUGUI subtitleText;   // "ЧУКОТКА · 1987"
    [SerializeField] private Image[]         decoLines;      // 2 горизонтальных линии

    // ── Меню ─────────────────────────────────────────────────────────────

    [Header("Кнопки меню")]
    [SerializeField] private CanvasGroup     menuGroup;
    [SerializeField] private RectTransform[] menuButtons;    // NewGame / Load / Quit

    // ── Тайминги ──────────────────────────────────────────────────────────

    [Header("Тайминги (секунды)")]
    [SerializeField] private float introDelay         = 0.35f;  // задержка до старта
    [SerializeField] private float crystalDuration    = 0.90f;  // появление кристалла
    [SerializeField] private float pauseAfterCrystal  = 0.22f;
    [SerializeField] private float titleDuration      = 0.65f;  // печать заголовка
    [SerializeField] private float linesDuration      = 0.45f;  // декор. линии
    [SerializeField] private float pauseAfterTitle    = 0.28f;
    [SerializeField] private float subtitleDuration   = 0.48f;  // субтитр
    [SerializeField] private float buttonDuration     = 0.38f;  // каждая кнопка
    [SerializeField] private float buttonStagger      = 0.10f;  // задержка между кнопками
    [SerializeField] private float buttonSlideOffset  = 48f;    // px — кнопки едут снизу

    // ── Idle ──────────────────────────────────────────────────────────────

    [Header("Idle-анимация")]
    [SerializeField] private float crystalPulseSpeed = 0.65f;
    [SerializeField] private float crystalPulseScale = 0.016f;

    // ── Runtime state ─────────────────────────────────────────────────────

    private Vector3 _crystalBase;
    private float   _pulsePhase;
    private bool    _idle;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        HideAll();
    }

    private void Start()
    {
        StartCoroutine(IntroSequence());
    }

    private void Update()
    {
        if (!_idle || crystalRect == null) return;

        _pulsePhase += Time.unscaledDeltaTime * crystalPulseSpeed;
        float t = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;
        float s = 1f + Mathf.Lerp(-crystalPulseScale, crystalPulseScale, t);
        crystalRect.localScale = _crystalBase * s;
    }

    // ── Скрыть всё перед стартом ──────────────────────────────────────────

    private void HideAll()
    {
        if (logoGroup != null)
        {
            logoGroup.alpha          = 0f;
            logoGroup.interactable   = false;
            logoGroup.blocksRaycasts = false;
        }
        if (menuGroup != null)
        {
            menuGroup.alpha          = 0f;
            menuGroup.interactable   = false;
            menuGroup.blocksRaycasts = false;
        }

        if (crystalRect != null)
        {
            _crystalBase           = crystalRect.localScale;
            crystalRect.localScale = Vector3.zero;
        }

        if (titleText != null)
        {
            titleText.alpha = 0f;
            titleText.ForceMeshUpdate();
            titleText.maxVisibleCharacters = 0;
        }

        if (subtitleText != null)
            subtitleText.alpha = 0f;

        if (decoLines != null)
            foreach (var line in decoLines)
                if (line != null) line.color = WithAlpha(line.color, 0f);

        // Смещаем кнопки вниз — они приедут снизу вверх
        if (menuButtons != null)
            foreach (var btn in menuButtons)
                if (btn != null) btn.anchoredPosition -= new Vector2(0f, buttonSlideOffset);
    }

    // ── Главная последовательность ────────────────────────────────────────

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSecondsRealtime(introDelay);

        // ── 1. Показываем группу логотипа
        if (logoGroup != null)
        {
            logoGroup.alpha          = 1f;
            logoGroup.interactable   = true;
            logoGroup.blocksRaycasts = true;
        }

        // ── 2. Кристалл — вылетает с EaseOutBack (с небольшим превышением)
        if (crystalRect != null)
            yield return StartCoroutine(
                AnimateScale(crystalRect, Vector3.zero, _crystalBase, crystalDuration, EaseOutBack));

        yield return new WaitForSecondsRealtime(pauseAfterCrystal);

        // ── 3. Заголовок — typewriter по символам
        if (titleText != null)
        {
            titleText.alpha = 1f;
            int total   = titleText.text.Length;
            float elapsed = 0f;
            while (elapsed < titleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                titleText.maxVisibleCharacters =
                    Mathf.RoundToInt(Mathf.Lerp(0, total, EaseOutCubic(elapsed / titleDuration)));
                yield return null;
            }
            titleText.maxVisibleCharacters = total;
        }

        // ── 4. Декоративные линии — появляются одновременно с заголовком
        if (decoLines != null)
        {
            float elapsed = 0f;
            while (elapsed < linesDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutCubic(elapsed / linesDuration);
                foreach (var line in decoLines)
                    if (line != null) line.color = WithAlpha(line.color, t);
                yield return null;
            }
            foreach (var line in decoLines)
                if (line != null) line.color = WithAlpha(line.color, 1f);
        }

        yield return new WaitForSecondsRealtime(pauseAfterTitle);

        // ── 5. Субтитр
        if (subtitleText != null)
            yield return StartCoroutine(FadeTMP(subtitleText, 0f, 1f, subtitleDuration));

        // ── 6. Кнопки — стагерный слайд снизу вверх
        if (menuGroup != null)
        {
            menuGroup.alpha          = 1f;
            menuGroup.interactable   = true;
            menuGroup.blocksRaycasts = true;
        }

        if (menuButtons != null)
        {
            for (int i = 0; i < menuButtons.Length; i++)
            {
                var btn = menuButtons[i];
                if (btn == null) continue;

                Vector2 targetPos = btn.anchoredPosition + new Vector2(0f, buttonSlideOffset);
                StartCoroutine(AnimateAnchoredPos(btn, btn.anchoredPosition, targetPos,
                                                  buttonDuration, EaseOutCubic));

                // Fade на CanvasGroup кнопки (добавь CanvasGroup на каждую кнопку)
                var cg = btn.GetComponent<CanvasGroup>();
                if (cg != null) StartCoroutine(FadeCanvasGroup(cg, 0f, 1f, buttonDuration));

                yield return new WaitForSecondsRealtime(buttonStagger);
            }
            yield return new WaitForSecondsRealtime(buttonDuration);
        }

        // ── Запускаем idle-анимацию кристалла
        _idle = true;
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    private IEnumerator AnimateScale(Transform t, Vector3 from, Vector3 to,
                                     float dur, System.Func<float, float> ease)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.LerpUnclamped(from, to, ease(Mathf.Clamp01(elapsed / dur)));
            yield return null;
        }
        t.localScale = to;
    }

    private IEnumerator AnimateAnchoredPos(RectTransform rt, Vector2 from, Vector2 to,
                                           float dur, System.Func<float, float> ease)
    {
        float elapsed = 0f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, ease(Mathf.Clamp01(elapsed / dur)));
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    private IEnumerator FadeTMP(TextMeshProUGUI tmp, float from, float to, float dur)
    {
        float elapsed = 0f;
        tmp.alpha = from;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            tmp.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        tmp.alpha = to;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float dur)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / dur);
            yield return null;
        }
        cg.alpha = to;
    }

    // ── Easing ────────────────────────────────────────────────────────────

    /// <summary>Отпружинивает за конечное значение и возвращается — эффект "шлепка".</summary>
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    /// <summary>Быстрый старт, плавное замедление.</summary>
    private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

    // ── Utils ─────────────────────────────────────────────────────────────

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }
}
