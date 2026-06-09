using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PowerNetworkOverlay
{
    // Включить/выключить через Tools → Power Network → Show Network Overlay
    private static bool _enabled = true;

    private static readonly Color ColorPowered   = new Color(0.2f, 1f,   0.2f,  0.8f);
    private static readonly Color ColorBroken    = new Color(1f,   0.15f, 0.15f, 0.9f);
    private static readonly Color ColorUnpowered = new Color(0.4f, 0.4f,  0.4f,  0.5f);
    private static readonly Color ColorEditMode  = new Color(0.3f, 0.7f,  1f,   0.5f);

    static PowerNetworkOverlay()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem("Tools/Power Network/Toggle Network Overlay")]
    private static void Toggle()
    {
        _enabled = !_enabled;
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/Power Network/Toggle Network Overlay", true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked("Tools/Power Network/Toggle Network Overlay", _enabled);
        return true;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_enabled) return;

        var cables = Object.FindObjectsByType<PowerCable>(FindObjectsSortMode.None);
        if (cables == null || cables.Length == 0) return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        foreach (var cable in cables)
        {
            if (cable == null || cable.nodeA == null || cable.nodeB == null) continue;

            Vector3 from = cable.nodeA.transform.position;
            Vector3 to   = cable.nodeB.transform.position;

            Color lineColor;
            float thickness;

            if (Application.isPlaying)
            {
                if (cable.isBroken)
                {
                    lineColor = ColorBroken;
                    thickness = 3f;
                }
                else if (cable.nodeA.IsPowered && cable.nodeB.IsPowered)
                {
                    lineColor = ColorPowered;
                    thickness = 3f;
                }
                else
                {
                    lineColor = ColorUnpowered;
                    thickness = 1.5f;
                }
            }
            else
            {
                lineColor = cable.isBroken ? ColorBroken : ColorEditMode;
                thickness = cable.isBroken ? 3f : 2f;
            }

            Handles.color = lineColor;
            Handles.DrawLine(from, to, thickness);

            // Иконка "сломан" в середине провода
            if (cable.isBroken)
            {
                Vector3 mid = (from + to) * 0.5f;
                Handles.Label(mid, "✗ сломан");
            }
        }

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
    }
}
