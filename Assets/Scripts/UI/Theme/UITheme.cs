using UnityEngine;

/// <summary>
/// Единая палитра и константы UI в стиле игры:
/// советский ЭЛТ-терминал (меню, инвентарь) + пожелтевшая бумага (записки).
///
/// Цвета доступны статически, чтобы любой UI-скрипт мог применять их в рантайме.
/// Спрайты генерируются через меню CrystalCastle/Generate UI Theme Sprites.
/// </summary>
public static class UITheme
{
    // ── Терминал (меню / инвентарь) ──────────────────────────────────────────

    /// Основной фосфор (янтарь советской аппаратуры).
    public static readonly Color Phosphor       = new Color32(0xE8, 0xA0, 0x2D, 0xFF);
    /// Яркий фосфор — заголовки, выделение.
    public static readonly Color PhosphorBright  = new Color32(0xFF, 0xD2, 0x7A, 0xFF);
    /// Тусклый фосфор — неактивные элементы, подписи.
    public static readonly Color PhosphorDim     = new Color32(0x8A, 0x6A, 0x24, 0xFF);

    /// Фон панели (почти чёрный сине-зелёный), полупрозрачный.
    public static readonly Color PanelBg         = new Color32(0x0A, 0x0E, 0x0C, 0xE0);
    /// Фон панели без прозрачности.
    public static readonly Color PanelBgSolid    = new Color32(0x0A, 0x0E, 0x0C, 0xFF);
    /// Цвет рамки/уголков.
    public static readonly Color Border          = new Color32(0xC8, 0x88, 0x1F, 0xFF);

    /// Затемнение фона за модальным окном.
    public static readonly Color Scrim           = new Color32(0x00, 0x00, 0x00, 0xC0);

    // ── Состояния ────────────────────────────────────────────────────────────

    public static readonly Color Warning  = new Color32(0xC0, 0x39, 0x2B, 0xFF); // тревога / ошибка
    public static readonly Color Success  = new Color32(0x5F, 0xD3, 0x5F, 0xFF); // успех / готово

    // ── Бумага (записки) ─────────────────────────────────────────────────────

    public static readonly Color PaperBg     = new Color32(0xD6, 0xC7, 0xA0, 0xFF); // выцветшая бумага
    public static readonly Color PaperInk    = new Color32(0x2A, 0x21, 0x18, 0xFF); // машинописный текст
    public static readonly Color PaperFaded  = new Color32(0x5A, 0x4A, 0x32, 0xFF); // бледный текст / подпись
    public static readonly Color PaperStamp  = new Color32(0x9B, 0x2C, 0x20, 0xFF); // штамп «СЕКРЕТНО»
    public static readonly Color PaperShadow = new Color32(0x3A, 0x2E, 0x1E, 0x60); // тень/края

    // ── Кнопки (терминал) ────────────────────────────────────────────────────
    // Спрайт кнопки (button_terminal) уже содержит рамку с прозрачным центром.
    // Цвета — это tint поверх спрайта: при наведении рамка теплеет, не заливая центр.

    public static Color ButtonNormal      => Color.white;                        // спрайт как есть
    public static Color ButtonHighlighted => new Color(1f, 0.82f, 0.45f, 1f);    // тёплая вспышка рамки
    public static Color ButtonPressed     => new Color(0.78f, 0.58f, 0.25f, 1f);
    public static Color ButtonDisabled    => new Color(0.4f, 0.4f, 0.4f, 0.5f);

    // ── Тайминги/настройки ───────────────────────────────────────────────────

    public const float FadeFast = 0.15f;
    public const float FadeSlow = 0.30f;

    /// Применить «терминальную» цветовую схему к UnityEngine.UI.Selectable (кнопка).
    public static void ApplyTerminalColors(UnityEngine.UI.Selectable selectable)
    {
        if (selectable == null) return;
        var c = selectable.colors;
        c.normalColor      = ButtonNormal;
        c.highlightedColor = ButtonHighlighted;
        c.pressedColor     = ButtonPressed;
        c.selectedColor    = ButtonHighlighted;
        c.disabledColor    = ButtonDisabled;
        c.fadeDuration     = 0.08f;
        c.colorMultiplier  = 1f;
        selectable.colors  = c;
    }
}
