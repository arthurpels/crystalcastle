using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Редакторная подсветка для работы в тёмных помещениях с ВЫКЛЮЧЕННОЙ лампочкой
/// Scene View. Поднимает ambient (его SampleSH в PSX_Lit читает независимо от
/// тумблера освещения), поэтому тёмные стены становятся видны.
///
/// Оригинальный ambient хранится в SessionState — переживает перекомпиляцию и
/// Play Mode, поэтому тулза НЕ может случайно запомнить собственное белое
/// значение как оригинал (был такой баг — ambient оставался белым после
/// выключения). В сцену ничего не пишется: перед сохранением ambient
/// откатывается к оригиналу.
///
/// Tools → Scene Lighting Helper
/// </summary>
public class SceneLightingHelper : EditorWindow
{
    // Ключи SessionState (живут в пределах сессии редактора, переживают reload/Play Mode)
    private const string KApplied   = "SceneLightingHelper.applied";
    private const string KMode      = "SceneLightingHelper.mode";
    private const string KR         = "SceneLightingHelper.r";
    private const string KG         = "SceneLightingHelper.g";
    private const string KB         = "SceneLightingHelper.b";
    private const string KA         = "SceneLightingHelper.a";
    private const string KIntensity = "SceneLightingHelper.intensity";

    [SerializeField] private bool  on;
    [SerializeField] private float ambientLevel = 0.6f;

    [MenuItem("Tools/Scene Lighting Helper")]
    private static void Open() => GetWindow<SceneLightingHelper>("Подсветка сцены");

    private void OnEnable()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
        EditorSceneManager.sceneSaved  += OnSceneSaved;

        if (on) ApplyAmbient();
        // Подчищаем рассинхрон: тумблер выключен, но где-то осталось применённое.
        else if (IsApplied) RestoreAmbient();
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneSaving -= OnSceneSaving;
        EditorSceneManager.sceneSaved  -= OnSceneSaved;
        // Снимаем эффект, но НЕ трогаем поле on — переживёт перекомпиляцию.
        RestoreAmbient();
    }

    // ── GUI ───────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Поднимает ambient для работы в тёмных помещениях\n" +
            "(работает с выключенной лампочкой Scene View).\n" +
            "Не влияет на игру и не сохраняется в сцену.", MessageType.Info);

        EditorGUI.BeginChangeCheck();

        bool newOn = GUILayout.Toggle(on,
            on ? "  Подсветка ВКЛ (нажми чтобы выключить)"
               : "  Подсветка ВЫКЛ (нажми чтобы включить)",
            "Button", GUILayout.Height(30));

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(!newOn))
            ambientLevel = EditorGUILayout.Slider("Уровень ambient", ambientLevel, 0f, 1.5f);

        if (EditorGUI.EndChangeCheck())
        {
            if (newOn != on)
            {
                on = newOn;
                if (on) ApplyAmbient();
                else    RestoreAmbient();
            }
            else if (on)
            {
                ApplyAmbient(); // живое обновление слайдера
            }
            SceneView.RepaintAll();
        }
    }

    // ── Применение / снятие ────────────────────────────────────────────────

    private static bool IsApplied => SessionState.GetBool(KApplied, false);

    private void ApplyAmbient()
    {
        // Захватываем оригинал ТОЛЬКО если ещё не применяли — иначе запомним
        // собственное белое значение.
        if (!IsApplied)
        {
            SessionState.SetInt(KMode, (int)RenderSettings.ambientMode);
            var c = RenderSettings.ambientLight;
            SessionState.SetFloat(KR, c.r);
            SessionState.SetFloat(KG, c.g);
            SessionState.SetFloat(KB, c.b);
            SessionState.SetFloat(KA, c.a);
            SessionState.SetFloat(KIntensity, RenderSettings.ambientIntensity);
            SessionState.SetBool(KApplied, true);
        }

        RenderSettings.ambientMode  = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(ambientLevel, ambientLevel, ambientLevel, 1f);
    }

    private void RestoreAmbient()
    {
        if (!IsApplied) return;

        RenderSettings.ambientMode  = (AmbientMode)SessionState.GetInt(KMode, (int)AmbientMode.Skybox);
        RenderSettings.ambientLight = new Color(
            SessionState.GetFloat(KR, 0f),
            SessionState.GetFloat(KG, 0f),
            SessionState.GetFloat(KB, 0f),
            SessionState.GetFloat(KA, 1f));
        RenderSettings.ambientIntensity = SessionState.GetFloat(KIntensity, 1f);

        SessionState.SetBool(KApplied, false);
    }

    // ── Защита от записи ambient в сцену при сохранении ─────────────────────

    private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (on) RestoreAmbient(); // в файл сцены уходят честные значения
    }

    private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
    {
        if (on) ApplyAmbient();
    }
}
