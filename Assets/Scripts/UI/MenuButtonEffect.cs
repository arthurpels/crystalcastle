using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Добавь на каждую кнопку главного меню (или на её RectTransform-родитель).
/// Реализует:
///   • плавный scale-up при наведении (EaseOutCubic)
///   • возврат к базовому scale при уходе курсора
///   • мягкая смена цвета (tint) при hover
///   • опциональные звуки hover / click
///
/// Работает на Time.unscaledDeltaTime — не зависит от timeScale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MenuButtonEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler
{
    // ── Scale ─────────────────────────────────────────────────────────────

    [Header("Scale")]
    [SerializeField] private float hoverScale  = 1.07f;   // масштаб при наведении
    [SerializeField] private float clickScale  = 0.94f;   // кратковременное «вдавливание»
    [SerializeField] private float lerpSpeed   = 12f;     // скорость интерполяции

    // ── Tint ──────────────────────────────────────────────────────────────

    [Header("Tint (опционально)")]
    [SerializeField] private Graphic targetGraphic;       // назначь Graphic кнопки; если null — ищем сами
    [SerializeField] private Color   normalColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color   hoverColor  = new Color(0.85f, 0.95f, 1f, 1f); // холодный голубоватый

    // ── Звуки ─────────────────────────────────────────────────────────────

    [Header("Звуки (опционально)")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 0.55f;

    // ── Runtime state ─────────────────────────────────────────────────────

    private RectTransform _rect;
    private Vector3       _baseScale;
    private float         _targetScaleMul = 1f;  // множитель (1 / hoverScale / clickScale)
    private bool          _isHovered;
    private bool          _isPressed;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        _rect      = GetComponent<RectTransform>();
        _baseScale = _rect.localScale;

        // Автоматически найти Graphic, если не назначен
        if (targetGraphic == null)
            targetGraphic = GetComponentInChildren<Graphic>();
    }

    private void OnEnable()
    {
        // Сбрасываем состояние при повторном включении объекта
        _isHovered      = false;
        _isPressed      = false;
        _targetScaleMul = 1f;
        _rect.localScale = _baseScale;

        if (targetGraphic != null)
            targetGraphic.color = normalColor;
    }

    private void Update()
    {
        // ── Плавный scale ──────────────────────────────────────────────────
        float currentMul = _rect.localScale.x /
                           (_baseScale.x > 0.0001f ? _baseScale.x : 1f);

        float newMul = Mathf.Lerp(currentMul, _targetScaleMul,
                                  1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime));

        _rect.localScale = _baseScale * newMul;

        // ── Плавный tint ───────────────────────────────────────────────────
        if (targetGraphic != null)
        {
            Color targetColor = _isHovered ? hoverColor : normalColor;
            targetGraphic.color = Color.Lerp(targetGraphic.color, targetColor,
                                             1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime));
        }
    }

    // ── Pointer events ────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData _)
    {
        _isHovered      = true;
        _targetScaleMul = _isPressed ? clickScale : hoverScale;

        if (hoverSound != null)
            AudioSource.PlayClipAtPoint(hoverSound, Vector3.zero, soundVolume);
    }

    public void OnPointerExit(PointerEventData _)
    {
        _isHovered      = false;
        _isPressed      = false;
        _targetScaleMul = 1f;
    }

    public void OnPointerDown(PointerEventData _)
    {
        _isPressed      = true;
        _targetScaleMul = clickScale;
    }

    public void OnPointerUp(PointerEventData _)
    {
        _isPressed      = false;
        _targetScaleMul = _isHovered ? hoverScale : 1f;
    }

    public void OnPointerClick(PointerEventData _)
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero, soundVolume);
    }
}
