using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Применяет терминальную тему к существующим UI-панелям.
/// Стилизует фон, кнопки, текст. Поддерживает Undo (Ctrl+Z) для объектов сцены/префаба.
///
/// Лучше запускать в режиме редактирования префаба (открой Canvas.prefab),
/// чтобы изменения сохранились в префаб, а не как override в сцене.
///
/// Меню: CrystalCastle/Apply Theme/...
/// </summary>
public static class UIThemeApplier
{
    private const string Dir = "Assets/Art/UI/Generated";

    // ── Меню паузы ───────────────────────────────────────────────────────────

    [MenuItem("CrystalCastle/Apply Theme/Pause Menu")]
    public static void ApplyPauseMenu()
    {
        var pm = Object.FindObjectOfType<PauseMenu>(true);
        if (pm == null) { NotFound("PauseMenu"); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        var so = new SerializedObject(pm);
        var panel  = GetObj(so, "panel") as GameObject;
        var resume = GetComp<Button>(so, "resumeButton");
        var save   = GetComp<Button>(so, "saveButton");
        var load   = GetComp<Button>(so, "loadButton");
        var quit   = GetComp<Button>(so, "quitButton");
        var status = GetComp<TextMeshProUGUI>(so, "statusText");

        var panelSprite  = Load("panel_terminal");
        var buttonSprite = Load("button_terminal");

        if (panel != null) StylePanel(panel, panelSprite);
        StyleButton(resume, buttonSprite);
        StyleButton(save,   buttonSprite);
        StyleButton(load,   buttonSprite);
        StyleButton(quit,   buttonSprite);
        if (status != null) StyleText(status, UITheme.PhosphorDim, 0f);

        if (panel != null)
        {
            AddTitle(panel, "ПАУЗА");
            AddScanlines(panel);
            EnsureFlicker(panel);
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log("[UIThemeApplier] Меню паузы стилизовано.");
    }

    // ── Инвентарь ────────────────────────────────────────────────────────────

    [MenuItem("CrystalCastle/Apply Theme/Inventory")]
    public static void ApplyInventory()
    {
        var inv = Object.FindObjectOfType<InventoryUI>(true);
        if (inv == null) { NotFound("InventoryUI"); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        var so = new SerializedObject(inv);
        var panel      = GetObj(so, "inventoryPanel") as GameObject;
        var slotPrefab = GetObj(so, "slotPrefab") as GameObject;

        if (panel != null)
        {
            StylePanel(panel, Load("panel_terminal"));
            AddTitle(panel, "ИНВЕНТАРЬ");
            AddScanlines(panel);
            EnsureFlicker(panel);
        }

        Undo.CollapseUndoOperations(group);

        // Слот — отдельный префаб-ассет, правим через LoadPrefabContents.
        if (slotPrefab != null) StyleSlotPrefab(slotPrefab);

        Debug.Log("[UIThemeApplier] Инвентарь стилизован.");
    }

    private static void StyleSlotPrefab(GameObject slotPrefabRef)
    {
        string path = AssetDatabase.GetAssetPath(slotPrefabRef);
        if (string.IsNullOrEmpty(path)) { Debug.LogWarning("[UIThemeApplier] slotPrefab не является ассетом."); return; }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // Фон ячейки на корне
            var bg = root.GetComponent<Image>();
            if (bg == null) bg = root.AddComponent<Image>();
            bg.sprite = Load("slot_terminal");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            bg.pixelsPerUnitMultiplier = 1f;
            bg.raycastTarget = false;

            var slot = root.GetComponent<ItemSlotUI>();
            if (slot != null)
            {
                var sso = new SerializedObject(slot);
                var nameText   = GetComp<TMP_Text>(sso, "nameText");
                var amountText = GetComp<TMP_Text>(sso, "amountText");
                var btnSprite  = Load("button_terminal");

                StyleButton(GetComp<Button>(sso, "dropButton"),       btnSprite, undo: false, charSpacing: 0f);
                StyleButton(GetComp<Button>(sso, "equipLeftButton"),  btnSprite, undo: false, charSpacing: 0f);
                StyleButton(GetComp<Button>(sso, "equipRightButton"), btnSprite, undo: false, charSpacing: 0f);
                StyleButton(GetComp<Button>(sso, "unequipButton"),    btnSprite, undo: false, charSpacing: 0f);
                StyleText(nameText,   UITheme.PhosphorBright, 1f, undo: false);
                StyleText(amountText, UITheme.Phosphor,       0f, undo: false);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ── Записки (бумага) ─────────────────────────────────────────────────────

    [MenuItem("CrystalCastle/Apply Theme/Document Reader")]
    public static void ApplyDocumentReader()
    {
        var dr = Object.FindObjectOfType<DocumentReaderUI>(true);
        if (dr == null) { NotFound("DocumentReaderUI"); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        var so = new SerializedObject(dr);
        var overlay = GetComp<CanvasGroup>(so, "overlay");
        var panel   = GetObj(so, "panel") as GameObject;
        var title   = GetComp<TMP_Text>(so, "titleText");
        var body    = GetComp<TMP_Text>(so, "bodyText");

        if (panel != null)   StylePaperPanel(panel);
        if (title != null)   StyleText(title, UITheme.PaperInk, 1f);
        if (body  != null)   StyleText(body,  UITheme.PaperInk, 0f);
        if (panel != null)   AddStamp(panel, "СЕКРЕТНО");
        if (overlay != null) AddVignette(overlay.gameObject);

        Undo.CollapseUndoOperations(group);
        Debug.Log("[UIThemeApplier] Записки стилизованы.");
    }

    public static void StylePaperPanel(GameObject go)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = Undo.AddComponent<Image>(go);
        else Undo.RecordObject(img, "Style Paper");

        img.sprite = Load("paper");
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = true;
        EditorUtility.SetDirty(img);
    }

    /// Штамп «СЕКРЕТНО» — рамка + повёрнутый выцветший текст в углу листа.
    public static void AddStamp(GameObject panel, string text)
    {
        if (panel.transform.Find("Stamp") != null) return;

        var go = new GameObject("Stamp", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Add Stamp");
        go.transform.SetParent(panel.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(-140f, -74f);
        rt.sizeDelta = new Vector2(230f, 86f);
        rt.localRotation = Quaternion.Euler(0, 0, -11f);

        var frame = go.GetComponent<Image>();
        frame.sprite = Load("stamp_frame");
        frame.type = Image.Type.Sliced;
        frame.color = Color.white;
        frame.pixelsPerUnitMultiplier = 1f;
        frame.raycastTarget = false;

        var txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform, false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        var t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 34;
        t.color = UITheme.PaperStamp;
        t.characterSpacing = 5f;
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        t.enableWordWrapping = false;
        var font = LoadFont();
        if (font != null) t.font = font;

        go.transform.SetAsLastSibling();
        EditorUtility.SetDirty(go);
    }

    /// Радиальное затемнение краёв на весь оверлей.
    public static void AddVignette(GameObject root)
    {
        if (root.transform.Find("Vignette") != null) return;

        var go = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Add Vignette");
        go.transform.SetParent(root.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.sprite = Load("vignette");
        img.type = Image.Type.Simple;
        img.color = Color.white;
        img.raycastTarget = false;

        go.transform.SetAsLastSibling();
        EditorUtility.SetDirty(go);
    }

    // ── Хелперы стилизации ───────────────────────────────────────────────────

    public static void StylePanel(GameObject go, Sprite sprite, bool undo = true)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = undo ? Undo.AddComponent<Image>(go) : go.AddComponent<Image>();
        else if (undo) Undo.RecordObject(img, "Style Panel");

        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = Color.white;
        img.pixelsPerUnitMultiplier = 1f;
        img.raycastTarget = true;
        EditorUtility.SetDirty(img);
    }

    public static void StyleButton(Button b, Sprite sprite, bool undo = true, float charSpacing = 3f)
    {
        if (b == null) return;
        if (undo) Undo.RecordObject(b, "Style Button");

        var img = b.targetGraphic as Image ?? b.GetComponent<Image>();
        if (img != null)
        {
            if (undo) Undo.RecordObject(img, "Style Button");
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = 1f;
            EditorUtility.SetDirty(img);
        }

        b.transition = Selectable.Transition.ColorTint;
        var c = b.colors;
        c.normalColor      = UITheme.ButtonNormal;
        c.highlightedColor = UITheme.ButtonHighlighted;
        c.pressedColor     = UITheme.ButtonPressed;
        c.selectedColor    = UITheme.ButtonHighlighted;
        c.disabledColor    = UITheme.ButtonDisabled;
        c.fadeDuration     = 0.08f;
        c.colorMultiplier  = 1f;
        b.colors = c;

        var txt = b.GetComponentInChildren<TMP_Text>(true);
        if (txt != null)
        {
            if (undo) Undo.RecordObject(txt, "Style Button Text");
            txt.color = UITheme.PhosphorBright;
            txt.fontStyle |= FontStyles.UpperCase;
            txt.characterSpacing = charSpacing;
            txt.enableWordWrapping = false;                       // в одну строку
            txt.overflowMode = TextOverflowModes.Overflow;
            EditorUtility.SetDirty(txt);
        }
        EditorUtility.SetDirty(b);
    }

    public static void StyleText(TMP_Text t, Color color, float spacing = 2f, bool undo = true)
    {
        if (t == null) return;
        if (undo) Undo.RecordObject(t, "Style Text");
        t.color = color;
        if (spacing > 0f) t.characterSpacing = spacing;
        EditorUtility.SetDirty(t);
    }

    public static void EnsureFlicker(GameObject panel)
    {
        if (panel.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(panel);
        if (panel.GetComponent<CRTFlicker>() == null)  Undo.AddComponent<CRTFlicker>(panel);
    }

    /// Заголовок на верхнем якоре панели (не зависит от раскладки кнопок).
    public static void AddTitle(GameObject panel, string text)
    {
        if (panel.transform.Find("Title") != null) return;

        var go = new GameObject("Title", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add Title");
        go.transform.SetParent(panel.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -16f);
        rt.sizeDelta = new Vector2(260f, 44f);

        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.alignment = TextAlignmentOptions.Center;
        t.fontSize = 30;
        t.color = UITheme.PhosphorBright;
        t.characterSpacing = 10f;
        t.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        var font = LoadFont();
        if (font != null) t.font = font;

        go.transform.SetAsFirstSibling();
        EditorUtility.SetDirty(go);
    }

    /// Полупрозрачная развёртка ЭЛТ на всю панель, поверх содержимого.
    public static void AddScanlines(GameObject panel)
    {
        if (panel.transform.Find("Scanlines") != null) return;

        var go = new GameObject("Scanlines", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Add Scanlines");
        go.transform.SetParent(panel.transform, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.AddComponent<Image>();
        img.sprite = Load("scanlines");
        img.type = Image.Type.Tiled;
        img.color = new Color(1f, 1f, 1f, 0.5f);
        img.raycastTarget = false;

        go.transform.SetAsLastSibling();
        EditorUtility.SetDirty(go);
    }

    // ── Утилиты ──────────────────────────────────────────────────────────────

    private static TMP_FontAsset LoadFont() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Roboto-Regular SDF.asset");

    private static Sprite Load(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{name}.png");
        if (s == null) Debug.LogWarning($"[UIThemeApplier] Спрайт не найден: {name}. Запусти Generate UI Theme Sprites.");
        return s;
    }

    private static Object GetObj(SerializedObject so, string prop)
    {
        var p = so.FindProperty(prop);
        return p != null ? p.objectReferenceValue : null;
    }

    private static T GetComp<T>(SerializedObject so, string prop) where T : Component
    {
        var p = so.FindProperty(prop);
        return p != null ? p.objectReferenceValue as T : null;
    }

    private static void NotFound(string what) =>
        EditorUtility.DisplayDialog("UI Theme",
            $"{what} не найден.\nОткрой сцену или префаб Canvas, где он есть.", "OK");
}
