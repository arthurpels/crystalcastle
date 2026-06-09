using UnityEditor;
using UnityEngine;

public static class PowerNodeGizmos
{
    private static readonly Color ColorPowered    = new Color(0.15f, 1f,   0.15f, 0.9f);
    private static readonly Color ColorUnpowered  = new Color(1f,   0.15f, 0.15f, 0.9f);
    private static readonly Color ColorEditMode   = new Color(0.3f,  0.7f,  1f,   0.7f);

    [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.InSelectionHierarchy)]
    static void DrawGizmo(PowerNode node, GizmoType type)
    {
        bool isSelected = (type & GizmoType.Selected) != 0;
        float radius = isSelected ? 0.18f : 0.12f;

        if (Application.isPlaying)
            Gizmos.color = node.IsPowered ? ColorPowered : ColorUnpowered;
        else
            Gizmos.color = ColorEditMode;

        Gizmos.DrawSphere(node.transform.position, radius);

        // Подпись — только на выделенном или вблизи камеры
        if (isSelected || IsCloseToCamera(node.transform.position, 12f))
        {
            string label = Application.isPlaying
                ? $"{node.name}\n{(node.IsPowered ? "⚡ запитан" : "✗ нет питания")}"
                : node.name;

            Handles.Label(node.transform.position + Vector3.up * (radius + 0.15f), label);
        }
    }

    private static bool IsCloseToCamera(Vector3 pos, float maxDist)
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null) return false;
        return Vector3.Distance(sv.camera.transform.position, pos) < maxDist;
    }
}
