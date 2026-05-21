using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI для выбора слота сохранения/загрузки.
///
/// Иерархия (минимальная):
///   SavePanel (этот компонент)
///     ├─ Slot0 ─ SaveButton, LoadButton, DeleteButton, InfoText
///     ├─ Slot1 ─ ...
///     └─ Slot2 ─ ...
///
/// Как открыть: SaveSlotUI.Instance.Open(mode) или вызови Open() из паузы.
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    public enum Mode { Save, Load }

    public static SaveSlotUI Instance { get; private set; }

    [System.Serializable]
    public struct SlotView
    {
        public Button       saveButton;
        public Button       loadButton;
        public Button       deleteButton;
        public TextMeshProUGUI infoText;
    }

    [SerializeField] private GameObject     panel;
    [SerializeField] private SlotView[]     slots = new SlotView[SaveManager.SlotCount];
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Строки UI")]
    [SerializeField] private string emptySlotLabel  = "— Пусто —";
    [SerializeField] private string saveTitle        = "Сохранить";
    [SerializeField] private string loadTitle        = "Загрузить";
    [SerializeField] private string confirmSaveText  = "Перезаписать слот {0}?";

    private Mode   _mode;
    private bool   _isOpen;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i; // capture for lambda
            slots[i].saveButton?.onClick  .AddListener(() => OnSaveClicked(idx));
            slots[i].loadButton?.onClick  .AddListener(() => OnLoadClicked(idx));
            slots[i].deleteButton?.onClick.AddListener(() => OnDeleteClicked(idx));
        }

        // Подписываемся на завершение загрузки чтобы закрыть панель
        SaveManager.OnAfterLoad += Close;

        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy() => SaveManager.OnAfterLoad -= Close;

    // ── Public API ─────────────────────────────────────────────────────────

    public void Open(Mode mode = Mode.Save)
    {
        _mode   = mode;
        _isOpen = true;
        if (panel != null) panel.SetActive(true);
        if (titleText != null) titleText.text = mode == Mode.Save ? saveTitle : loadTitle;
        RefreshAll();
    }

    public void Close()
    {
        _isOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    public void Toggle(Mode mode = Mode.Save)
    {
        if (_isOpen) Close();
        else Open(mode);
    }

    // ── Slot events ────────────────────────────────────────────────────────

    private void OnSaveClicked(int slot)
    {
        // TODO: показать диалог подтверждения если слот занят
        SaveManager.Instance?.Save(slot);
        RefreshAll();
    }

    private void OnLoadClicked(int slot)
    {
        if (SaveManager.Instance == null || !SaveManager.Instance.SlotExists(slot)) return;
        SaveManager.Instance.Load(slot);
        // Панель закроется через OnAfterLoad
    }

    private void OnDeleteClicked(int slot)
    {
        SaveManager.Instance?.DeleteSlot(slot);
        RefreshAll();
    }

    // ── Refresh ────────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        for (int i = 0; i < slots.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int slot)
    {
        if (SaveManager.Instance == null) return;
        var view = slots[slot];
        bool exists = SaveManager.Instance.SlotExists(slot);

        // Текст слота
        if (view.infoText != null)
        {
            if (exists)
            {
                var meta = SaveManager.Instance.PeekSlot(slot);
                view.infoText.text = meta != null
                    ? $"[{slot + 1}] {meta.sceneName}  {meta.saveTime}"
                    : $"[{slot + 1}] {emptySlotLabel}";
            }
            else
            {
                view.infoText.text = $"[{slot + 1}] {emptySlotLabel}";
            }
        }

        // Кнопки
        bool isSaveMode = _mode == Mode.Save;
        view.saveButton?.gameObject  .SetActive(isSaveMode);
        view.loadButton?.gameObject  .SetActive(!isSaveMode && exists);
        view.deleteButton?.gameObject.SetActive(exists);
    }
}
