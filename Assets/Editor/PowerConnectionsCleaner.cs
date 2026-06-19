using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Чинит мусор в PowerNode.connections, накопившийся от Ctrl+D (копия таскает
/// чужие ссылки на кабели). Пересобирает список у каждой ноды строго по РЕАЛЬНЫМ
/// кабелям — берёт все PowerCable, чьи nodeA/nodeB указывают на эту ноду.
/// Выкидывает левые ссылки, дубли и null.
///
/// Tools → Power → Rebuild Node Connections
/// </summary>
public static class PowerConnectionsCleaner
{
    [MenuItem("Tools/Power/Rebuild Node Connections (from cables)")]
    private static void Rebuild()
    {
        var nodes  = Object.FindObjectsByType<PowerNode>(FindObjectsSortMode.None);
        var cables = Object.FindObjectsByType<PowerCable>(FindObjectsSortMode.None);

        // Истина: нода → кабели, которые реально на неё ссылаются.
        var real = new Dictionary<PowerNode, List<PowerCable>>();
        foreach (var n in nodes) real[n] = new List<PowerCable>();

        foreach (var c in cables)
        {
            if (c == null) continue;
            if (c.nodeA != null && real.TryGetValue(c.nodeA, out var la) && !la.Contains(c)) la.Add(c);
            if (c.nodeB != null && real.TryGetValue(c.nodeB, out var lb) && !lb.Contains(c)) lb.Add(c);
        }

        int changed = 0, removed = 0;
        foreach (var n in nodes)
        {
            var want = real[n];
            var have = n.connections ?? new List<PowerCable>();

            // Сравнение как множеств (порядок неважен для BFS).
            bool same = have.Count == want.Count
                        && !want.Exists(c => !have.Contains(c))
                        && !have.Exists(c => !want.Contains(c));
            if (same) continue;

            removed += Mathf.Max(0, have.Count - want.Count);
            Undo.RecordObject(n, "Rebuild Node Connections");
            n.connections = new List<PowerCable>(want);
            EditorUtility.SetDirty(n);
            changed++;
        }

        if (changed > 0)
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[PowerConnections] Пересобрано у {changed} нод (всего {nodes.Length}), " +
                  $"кабелей {cables.Length}. Лишних ссылок убрано ~{removed}. " +
                  (changed > 0 ? "Не забудь сохранить сцену (Ctrl+S)." : "Всё уже чисто."));
    }
}
