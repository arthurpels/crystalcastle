using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Меняет шейдер у материалов на один из наших (CrystalCastle/*), УМНО перенося
/// albedo-текстуру, цвет и тайлинг из стороннего шейдера в _BaseMap/_BaseColor.
/// При обычной смене шейдера Unity сбрасывает текстуру, если имена полей не
/// совпадают — эта тулза вытаскивает их ДО смены и проставляет ПОСЛЕ.
///
/// Работает по: выделенным материалам в Project + материалам на выделенных
/// объектах сцены (Renderer.sharedMaterials).
///
/// Tools → Material Shader Converter
/// </summary>
public class MaterialShaderConverter : EditorWindow
{
    // Кандидаты на albedo-текстуру (по убыванию вероятности)
    private static readonly string[] AlbedoProps =
    {
        "_BaseMap", "_MainTex", "_BaseColorMap", "_BaseColorTexture",
        "_Albedo", "_AlbedoMap", "_DiffuseMap", "_Diffuse",
        "_BaseTexture", "_MainTexture", "_Texture", "_Tex",
    };

    // Кандидаты на основной цвет
    private static readonly string[] ColorProps =
    {
        "_BaseColor", "_Color", "_BaseColorTint", "_Tint", "_MainColor",
    };

    private string[] _ourShaderNames;
    private int      _selectedShader;
    private Vector2  _scroll;

    [MenuItem("Tools/Material Shader Converter")]
    private static void Open() => GetWindow<MaterialShaderConverter>("Конвертер шейдеров");

    private void OnEnable() => RefreshShaderList();

    private void RefreshShaderList()
    {
        // Собираем все шейдеры из неймспейса CrystalCastle/ (новые появятся сами).
        _ourShaderNames = AssetDatabase.FindAssets("t:Shader")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<Shader>)
            .Where(s => s != null && s.name.StartsWith("CrystalCastle/"))
            .Select(s => s.name)
            .OrderBy(n => n)
            .ToArray();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Меняет шейдер на наш, сохраняя albedo-текстуру, цвет и тайлинг.\n" +
            "Выдели материалы в Project или объекты в сцене и нажми Конвертировать.",
            MessageType.Info);

        if (_ourShaderNames == null || _ourShaderNames.Length == 0)
        {
            EditorGUILayout.HelpBox("Не найдено шейдеров CrystalCastle/*.", MessageType.Warning);
            if (GUILayout.Button("Обновить список")) RefreshShaderList();
            return;
        }

        _selectedShader = EditorGUILayout.Popup("Целевой шейдер", _selectedShader, _ourShaderNames);
        if (GUILayout.Button("Обновить список шейдеров")) RefreshShaderList();

        EditorGUILayout.Space();

        var materials = GatherSelectedMaterials();
        EditorGUILayout.LabelField($"Найдено материалов: {materials.Count}", EditorStyles.boldLabel);

        if (materials.Count > 0)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(160));
            foreach (var m in materials)
            {
                var shaderName = m.shader != null ? m.shader.name : "—";
                EditorGUILayout.LabelField($"• {m.name}", $"({shaderName})");
            }
            EditorGUILayout.EndScrollView();
        }

        using (new EditorGUI.DisabledScope(materials.Count == 0))
        {
            if (GUILayout.Button("Конвертировать", GUILayout.Height(30)))
                Convert(materials, Shader.Find(_ourShaderNames[_selectedShader]));
        }
    }

    // ── Сбор материалов ─────────────────────────────────────────────────────

    private static List<Material> GatherSelectedMaterials()
    {
        var set = new HashSet<Material>();

        // 1. Прямо выделенные материалы-ассеты в Project.
        foreach (var m in Selection.GetFiltered<Material>(SelectionMode.DeepAssets))
            if (m != null) set.Add(m);

        // 2. Материалы на выделенных объектах сцены.
        foreach (var go in Selection.gameObjects)
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                    if (m != null) set.Add(m);

        return set.ToList();
    }

    // ── Конвертация ─────────────────────────────────────────────────────────

    private static void Convert(List<Material> materials, Shader target)
    {
        if (target == null)
        {
            Debug.LogError("[ShaderConverter] Целевой шейдер не найден.");
            return;
        }

        int converted = 0, missingTex = 0;
        Undo.RecordObjects(materials.ToArray(), "Convert Material Shaders");

        foreach (var mat in materials)
        {
            if (mat.shader == target) continue; // уже наш

            // 1. Вытаскиваем данные ДО смены шейдера.
            var (albedo, scale, offset) = ExtractAlbedo(mat);
            Color color = ExtractColor(mat);

            // 2. Меняем шейдер.
            mat.shader = target;

            // 3. Возвращаем данные в наши поля (_BaseMap через [MainTexture]).
            if (albedo != null)
            {
                mat.mainTexture       = albedo;
                mat.mainTextureScale  = scale;
                mat.mainTextureOffset = offset;
            }
            else missingTex++;

            mat.color = color;

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ShaderConverter] Сконвертировано: {converted} материалов на «{target.name}». " +
                  (missingTex > 0 ? $"Без albedo-текстуры: {missingTex}." : "Все с текстурами."));
    }

    // ── Извлечение albedo / цвета ────────────────────────────────────────────

    private static (Texture tex, Vector2 scale, Vector2 offset) ExtractAlbedo(Material mat)
    {
        // Сначала пробуем Unity-шный mainTexture ([MainTexture]/_MainTex).
        if (mat.mainTexture != null)
            return (mat.mainTexture, mat.mainTextureScale, mat.mainTextureOffset);

        // Затем перебираем известные имена полей.
        foreach (var prop in AlbedoProps)
        {
            if (!mat.HasProperty(prop)) continue;
            var tex = mat.GetTexture(prop);
            if (tex != null)
                return (tex, mat.GetTextureScale(prop), mat.GetTextureOffset(prop));
        }

        return (null, Vector2.one, Vector2.zero);
    }

    private static Color ExtractColor(Material mat)
    {
        foreach (var prop in ColorProps)
            if (mat.HasProperty(prop))
                return mat.GetColor(prop);

        return Color.white; // нет цвета — не затемняем
    }
}
