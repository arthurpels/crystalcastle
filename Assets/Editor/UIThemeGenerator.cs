using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Процедурно генерирует набор UI-спрайтов в стиле игры
/// (советский ЭЛТ-терминал + пожелтевшая бумага) и импортирует их
/// с правильными 9-slice границами и point-фильтром под PS1.
///
/// Меню: CrystalCastle/Generate UI Theme Sprites
/// Результат: Assets/Art/UI/Generated/*.png
/// </summary>
public static class UIThemeGenerator
{
    private const string OutDir = "Assets/Art/UI/Generated";

    // Палитра дублирует UITheme (Editor не всегда видит рантайм-сборку при первом импорте).
    static readonly Color32 Phosphor      = new Color32(0xE8, 0xA0, 0x2D, 0xFF);
    static readonly Color32 PhosphorBright = new Color32(0xFF, 0xD2, 0x7A, 0xFF);
    static readonly Color32 PhosphorDim    = new Color32(0x8A, 0x6A, 0x24, 0xFF);
    static readonly Color32 PanelBg        = new Color32(0x0A, 0x0E, 0x0C, 0xE0);
    static readonly Color32 SlotBg         = new Color32(0x12, 0x18, 0x15, 0xD0);
    static readonly Color32 PaperBg        = new Color32(0xD6, 0xC7, 0xA0, 0xFF);
    static readonly Color32 PaperShadow    = new Color32(0x3A, 0x2E, 0x1E, 0xFF);

    [MenuItem("CrystalCastle/Generate UI Theme Sprites")]
    public static void Generate()
    {
        Directory.CreateDirectory(OutDir);

        SavePanelTerminal();   // фон панели меню/инвентаря (9-slice)
        SaveSlotTerminal();    // ячейка инвентаря (9-slice)
        SaveButtonTerminal();  // кнопка терминала (9-slice)
        SaveScanlines();       // тайл сканлайнов (repeat)
        SaveDivider();         // горизонтальный разделитель
        SaveVignette();        // радиальное затемнение краёв экрана
        SavePaper();           // лист бумаги для записок (9-slice)
        SaveStamp();           // рамка штампа «СЕКРЕТНО» (9-slice)

        AssetDatabase.Refresh();
        Debug.Log($"[UIThemeGenerator] Спрайты сгенерированы в {OutDir}");
        EditorUtility.RevealInFinder(OutDir);
    }

    // ── Спрайты ────────────────────────────────────────────────────────────

    /// Тёмная полупрозрачная панель с рамкой и яркими L-уголками.
    static void SavePanelTerminal()
    {
        const int S = 64, B = 20;
        var buf = New(S, S, new Color32(0, 0, 0, 0));
        FillRect(buf, S, B, B, S - B, S - B, PanelBg);            // центр (растяжимый)
        // Рамка по периметру (тусклая)
        Border(buf, S, 0, 0, S, S, 2, WithA(Phosphor, 90));
        // Яркие угловые скобки внутри нерастяжимых зон
        int len = 16, th = 2;
        DrawCorner(buf, S, 2, 2, len, th, Phosphor, +1, +1);             // низ-лево
        DrawCorner(buf, S, S - 3, 2, len, th, Phosphor, -1, +1);         // низ-право
        DrawCorner(buf, S, 2, S - 3, len, th, Phosphor, +1, -1);         // верх-лево
        DrawCorner(buf, S, S - 3, S - 3, len, th, Phosphor, -1, -1);     // верх-право
        Save("panel_terminal", buf, S, S, new Vector4(B, B, B, B), FilterMode.Point, TextureWrapMode.Clamp);
    }

    /// Ячейка инвентаря — рамка с угловыми насечками.
    static void SaveSlotTerminal()
    {
        const int S = 48, B = 14;
        var buf = New(S, S, new Color32(0, 0, 0, 0));
        FillRect(buf, S, 0, 0, S, S, SlotBg);
        Border(buf, S, 0, 0, S, S, 1, WithA(Phosphor, 70));
        int len = 9, th = 2;
        DrawCorner(buf, S, 1, 1, len, th, WithA(Phosphor, 200), +1, +1);
        DrawCorner(buf, S, S - 2, 1, len, th, WithA(Phosphor, 200), -1, +1);
        DrawCorner(buf, S, 1, S - 2, len, th, WithA(Phosphor, 200), +1, -1);
        DrawCorner(buf, S, S - 2, S - 2, len, th, WithA(Phosphor, 200), -1, -1);
        Save("slot_terminal", buf, S, S, new Vector4(B, B, B, B), FilterMode.Point, TextureWrapMode.Clamp);
    }

    /// Кнопка — прозрачный центр (под hover-заливку) + рамка и акцент снизу.
    static void SaveButtonTerminal()
    {
        const int S = 32, B = 10;
        var buf = New(S, S, new Color32(0, 0, 0, 0));
        Border(buf, S, 0, 0, S, S, 1, WithA(Phosphor, 80));      // тонкая рамка
        FillRect(buf, S, 0, 0, S, 2, WithA(Phosphor, 180));      // яркая нижняя грань
        Save("button_terminal", buf, S, S, new Vector4(B, B, B, B), FilterMode.Point, TextureWrapMode.Clamp);
    }

    /// Тайл сканлайнов: тёмная линия каждые 3 px.
    static void SaveScanlines()
    {
        const int S = 4;
        var buf = New(S, S, new Color32(0, 0, 0, 0));
        for (int x = 0; x < S; x++)
            SetPx(buf, S, x, 0, new Color32(0, 0, 0, 70)); // одна линия из трёх — тёмная
        Save("scanlines", buf, S, S, Vector4.zero, FilterMode.Point, TextureWrapMode.Repeat);
    }

    /// Горизонтальный разделитель: фосфор ярче в центре, гаснет к краям.
    static void SaveDivider()
    {
        const int W = 64, H = 4;
        var buf = New(W, H, new Color32(0, 0, 0, 0));
        for (int x = 0; x < W; x++)
        {
            float t = 1f - Mathf.Abs(x - (W - 1) / 2f) / ((W - 1) / 2f); // 0 края → 1 центр
            byte a = (byte)(Mathf.Pow(t, 1.5f) * 220);
            SetPx(buf, W, x, 1, WithA(Phosphor, a));
            SetPx(buf, W, x, 2, WithA(Phosphor, a));
        }
        Save("divider", buf, W, H, new Vector4(2, 0, 2, 0), FilterMode.Bilinear, TextureWrapMode.Clamp);
    }

    /// Радиальная виньетка: прозрачный центр → чёрные углы. Растягивается на экран.
    static void SaveVignette()
    {
        const int S = 256;
        var buf = New(S, S, new Color32(0, 0, 0, 0));
        Vector2 c = new Vector2((S - 1) / 2f, (S - 1) / 2f);
        float maxD = c.magnitude;
        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c) / maxD; // 0 центр → 1 угол
            float v = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, d));
            SetPx(buf, S, x, y, new Color32(0, 0, 0, (byte)(v * 200)));
        }
        Save("vignette", buf, S, S, Vector4.zero, FilterMode.Bilinear, TextureWrapMode.Clamp);
    }

    /// Лист бумаги: базовый тон + шум + виньетка по краям + пара пятен.
    static void SavePaper()
    {
        const int S = 128, B = 28;
        var buf = New(S, S, PaperBg);
        Vector2 c = new Vector2((S - 1) / 2f, (S - 1) / 2f);
        float maxD = c.magnitude;
        float seed = Random.value * 1000f;

        for (int y = 0; y < S; y++)
        for (int x = 0; x < S; x++)
        {
            // Многооктавный шум для неровности тона
            float n = Mathf.PerlinNoise((x + seed) * 0.06f, (y + seed) * 0.06f) * 0.6f
                    + Mathf.PerlinNoise((x + seed) * 0.18f, (y + seed) * 0.18f) * 0.4f;
            float shade = Mathf.Lerp(-16f, 10f, n);

            // Затемнение к краям (старая бумага темнее по периметру)
            float d = Vector2.Distance(new Vector2(x, y), c) / maxD;
            float edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 1.05f, d));

            Color32 baseC = PaperBg;
            byte r = (byte)Mathf.Clamp(baseC.r + shade - edge * 70, 0, 255);
            byte g = (byte)Mathf.Clamp(baseC.g + shade - edge * 64, 0, 255);
            byte bl = (byte)Mathf.Clamp(baseC.b + shade - edge * 50, 0, 255);
            SetPx(buf, S, x, y, new Color32(r, g, bl, 255));
        }

        // Несколько тёмных пятен (грязь/влага)
        for (int i = 0; i < 5; i++)
        {
            int sx = Random.Range(B, S - B), sy = Random.Range(B, S - B);
            int rad = Random.Range(4, 10);
            for (int y = -rad; y <= rad; y++)
            for (int x = -rad; x <= rad; x++)
            {
                float dd = Mathf.Sqrt(x * x + y * y) / rad;
                if (dd > 1f) continue;
                int px = sx + x, py = sy + y;
                Color32 cur = Get(buf, S, px, py);
                float k = (1f - dd) * 0.25f;
                SetPx(buf, S, px, py, new Color32(
                    (byte)Mathf.Lerp(cur.r, PaperShadow.r, k),
                    (byte)Mathf.Lerp(cur.g, PaperShadow.g, k),
                    (byte)Mathf.Lerp(cur.b, PaperShadow.b, k), 255));
            }
        }

        Save("paper", buf, S, S, new Vector4(B, B, B, B), FilterMode.Bilinear, TextureWrapMode.Clamp);
    }

    /// Двойная прямоугольная рамка штампа с потёртостями (выцветший красный).
    static void SaveStamp()
    {
        const int W = 160, H = 64, B = 14;
        var buf = New(W, H, new Color32(0, 0, 0, 0));
        Color32 st = new Color32(0x9B, 0x2C, 0x20, 0xFF);

        Border(buf, W, 0, 0, W, H, 3, st);              // внешняя толстая рамка
        Border(buf, W, 7, 7, W - 7, H - 7, 2, st);      // внутренняя тонкая

        // Потёртости — случайно гасим часть пикселей рамки (старая печать)
        for (int i = 0; i < buf.Length; i++)
            if (buf[i].a > 0 && Random.value < 0.20f)
                buf[i] = new Color32(buf[i].r, buf[i].g, buf[i].b, (byte)(buf[i].a * 0.25f));

        Save("stamp_frame", buf, W, H, new Vector4(B, 0, B, 0), FilterMode.Point, TextureWrapMode.Clamp);
    }

    // ── Рисование ────────────────────────────────────────────────────────────

    static Color32[] New(int w, int h, Color32 fill)
    {
        var buf = new Color32[w * h];
        for (int i = 0; i < buf.Length; i++) buf[i] = fill;
        return buf;
    }

    static void SetPx(Color32[] buf, int w, int x, int y, Color32 col)
    {
        if (x < 0 || y < 0 || x >= w || y >= buf.Length / w) return;
        // Альфа-композитинг поверх существующего
        Color32 dst = buf[y * w + x];
        float sa = col.a / 255f;
        buf[y * w + x] = new Color32(
            (byte)(col.r * sa + dst.r * (1 - sa)),
            (byte)(col.g * sa + dst.g * (1 - sa)),
            (byte)(col.b * sa + dst.b * (1 - sa)),
            (byte)Mathf.Min(255, col.a + dst.a * (1 - sa)));
    }

    static Color32 Get(Color32[] buf, int w, int x, int y)
    {
        if (x < 0 || y < 0 || x >= w || y >= buf.Length / w) return new Color32(0, 0, 0, 0);
        return buf[y * w + x];
    }

    static void FillRect(Color32[] buf, int w, int x0, int y0, int x1, int y1, Color32 col)
    {
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                SetPx(buf, w, x, y, col);
    }

    static void Border(Color32[] buf, int w, int x0, int y0, int x1, int y1, int th, Color32 col)
    {
        for (int t = 0; t < th; t++)
        {
            for (int x = x0; x < x1; x++) { SetPx(buf, w, x, y0 + t, col); SetPx(buf, w, x, y1 - 1 - t, col); }
            for (int y = y0; y < y1; y++) { SetPx(buf, w, x0 + t, y, col); SetPx(buf, w, x1 - 1 - t, y, col); }
        }
    }

    /// L-образная скобка: от (cx,cy) внутрь по направлениям (dx,dy).
    static void DrawCorner(Color32[] buf, int w, int cx, int cy, int len, int th, Color32 col, int dx, int dy)
    {
        for (int t = 0; t < th; t++)
        {
            for (int i = 0; i < len; i++)
            {
                SetPx(buf, w, cx + dx * i, cy + dy * t, col); // горизонтальный штрих
                SetPx(buf, w, cx + dx * t, cy + dy * i, col); // вертикальный штрих
            }
        }
    }

    static Color32 WithA(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);

    // ── Импорт ───────────────────────────────────────────────────────────────

    static void Save(string name, Color32[] buf, int w, int h, Vector4 border, FilterMode filter, TextureWrapMode wrap)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(buf);
        tex.Apply();

        string path = $"{OutDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType         = TextureImporterType.Sprite;
            imp.spriteImportMode    = SpriteImportMode.Single;
            imp.filterMode          = filter;
            imp.wrapMode            = wrap;
            imp.mipmapEnabled       = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.spritePixelsPerUnit = 100;
            if (border != Vector4.zero) imp.spriteBorder = border;
            imp.SaveAndReimport();
        }
    }
}
