using UnityEngine;

/// <summary>
/// Единая точка управления курсором.
/// Поддерживает режимы: Locked (геймплей), Free (UI), Hidden (катсцены).
/// </summary>
public class CursorController : MonoBehaviour
{
    public enum CursorMode
    {
        Locked,    // Геймплей: невидим, зацентрен, бесконечное вращение
        Free,      // UI/Инвентарь: виден, кликабелен, в пределах окна
        Hidden     // Катсцена: скрыт, но не зацентрен (для контроллеров)
    }

    [Header("Настройки")]
    [SerializeField] private CursorMode defaultMode = CursorMode.Locked;
    [SerializeField] private bool lockWhenOutOfFocus = true;

    private CursorMode _currentMode;
    private CursorMode? _forcedMode; // Приоритетный режим (для пауз, диалогов)

    private void Start()
    {
        SetMode(defaultMode);
        Application.focusChanged += OnFocusChanged;
    }

    private void OnDestroy()
    {
        Application.focusChanged -= OnFocusChanged;
    }

    /// <summary>
    /// Установить режим курсора (если нет приоритетного)
    /// </summary>
    public void SetMode(CursorMode mode)
    {
        if (_forcedMode.HasValue) return; // Игнорируем, если есть приоритет
        ApplyMode(mode);
        _currentMode = mode;
    }

    /// <summary>
    /// Принудительно установить режим (игнорирует обычные запросы)
    /// Используется для пауз, диалогов, меню
    /// </summary>
    public void ForceMode(CursorMode mode)
    {
        _forcedMode = mode;
        ApplyMode(mode);
    }

    /// <summary>
    /// Сбросить принудительный режим, вернуться к обычному
    /// </summary>
    public void ReleaseForce()
    {
        _forcedMode = null;
        ApplyMode(_currentMode);
    }

    /// <summary>
    /// Быстрые методы для типичных сценариев
    /// </summary>
    public void LockForGameplay() => SetMode(CursorMode.Locked);
    public void UnlockForUI() => SetMode(CursorMode.Free);
    public void HideForCutscene() => SetMode(CursorMode.Hidden);

    private void ApplyMode(CursorMode mode)
    {
        switch (mode)
        {
            case CursorMode.Locked:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
            case CursorMode.Free:
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                break;
            case CursorMode.Hidden:
                Cursor.lockState = CursorLockMode.Confined; // Или None, если нужно
                Cursor.visible = false;
                break;
        }
    }

    private void OnFocusChanged(bool hasFocus)
    {
        // Если окно потеряло фокус — всегда лочим курсор (защита от "улетевшей" мыши)
        if (!hasFocus && lockWhenOutOfFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        // При возврате фокуса — восстанавливаем режим
        else if (hasFocus)
        {
            ApplyMode(_forcedMode ?? _currentMode);
        }
    }
}