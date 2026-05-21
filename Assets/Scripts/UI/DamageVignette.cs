using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полноэкранная виньетка, реагирующая на урон игрока.
///
/// Два режима:
///   — Flash: яркая красная вспышка при каждом ударе, быстро гаснет.
///   — LowHealth: тусклая постоянная пульсация когда HP < lowHealthThreshold.
///
/// Иерархия:
///   Canvas (ScreenSpace-Overlay, поверх HUD)
///     └─ DamageVignetteImage  ← Image, source texture = vignetteSprite,
///                                цвет = красный, raycastTarget = false.
///                                Этот компонент — на том же объекте.
///
/// Назначь vignetteSprite: радиальный градиент от прозрачного центра
/// к непрозрачным краям (можно сгенерить в Photoshop / GIMP).
/// Без спрайта работает как простое затемнение через solid color.
/// </summary>
[RequireComponent(typeof(Image))]
public class DamageVignette : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private PlayerAttributes player;

    [Header("Вспышка при ударе")]
    [SerializeField] private Color   flashColor      = new Color(0.8f, 0f, 0f, 0.55f);
    [SerializeField] private float   flashDuration   = 0.12f;  // время нарастания
    [SerializeField] private float   flashFadeOut    = 0.45f;  // время угасания

    [Header("Низкое здоровье (пульсация)")]
    [SerializeField] private Color   lowHealthColor  = new Color(0.6f, 0f, 0f, 0.30f);
    [SerializeField] [Range(0f, 1f)]
    private float   lowHealthThreshold = 0.30f; // доля HP (0.3 = 30%)
    [SerializeField] private float   pulseSpeed      = 1.4f;

    [Header("Звук при уроне (опционально)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] hurtClips;

    // ── Runtime ───────────────────────────────────────────────────────────
    private Image      _img;
    private Color      _currentFlash = Color.clear;
    private Coroutine  _flashRoutine;
    private float      _pulsePhase;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        _img = GetComponent<Image>();
        _img.raycastTarget = false;
        _img.color = Color.clear;

        if (player == null) player = FindObjectOfType<PlayerAttributes>();
    }

    private void OnEnable()
    {
        if (player != null) player.OnDamaged += OnPlayerDamaged;
    }

    private void OnDisable()
    {
        if (player != null) player.OnDamaged -= OnPlayerDamaged;
    }

    private void Update()
    {
        bool isLow = player != null && player.GetHealthPercent() < lowHealthThreshold
                                    && player.IsAlive;

        Color target;

        if (isLow)
        {
            // Синусоидальная пульсация
            _pulsePhase += Time.deltaTime * pulseSpeed;
            float t = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f; // 0..1
            target = Color.Lerp(Color.clear, lowHealthColor, t);
        }
        else
        {
            target = Color.clear;
            _pulsePhase = 0f;
        }

        // Flash перебивает low-health
        if (_currentFlash.a > 0f)
        {
            // Выбираем более насыщенный из двух
            target = _currentFlash.a > target.a ? _currentFlash : target;
        }

        _img.color = target;
    }

    // ── Callbacks ─────────────────────────────────────────────────────────

    private void OnPlayerDamaged(float amount)
    {
        PlayHurtSound();

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        // Нарастание
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            _currentFlash = Color.Lerp(Color.clear, flashColor, elapsed / flashDuration);
            yield return null;
        }
        _currentFlash = flashColor;

        // Угасание
        elapsed = 0f;
        while (elapsed < flashFadeOut)
        {
            elapsed += Time.deltaTime;
            _currentFlash = Color.Lerp(flashColor, Color.clear, elapsed / flashFadeOut);
            yield return null;
        }
        _currentFlash = Color.clear;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private void PlayHurtSound()
    {
        if (audioSource == null || hurtClips == null || hurtClips.Length == 0) return;
        var clip = hurtClips[Random.Range(0, hurtClips.Length)];
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(clip);
    }
}
