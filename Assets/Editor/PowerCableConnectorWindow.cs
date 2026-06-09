using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PowerCableConnectorWindow : EditorWindow
{
    [SerializeField] private GameObject cablePrefab;

    private SerializedObject   _so;
    private SerializedProperty _prefabProp;

    [MenuItem("Tools/Power Network/Cable Connector")]
    public static void Open() => GetWindow<PowerCableConnectorWindow>("Cable Connector");

    private void OnEnable()
    {
        _so        = new SerializedObject(this);
        _prefabProp = _so.FindProperty("cablePrefab");
    }

    private void OnGUI()
    {
        _so.Update();
        EditorGUILayout.LabelField("Power Cable Connector", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(_prefabProp, new GUIContent("Cable Prefab"));
        _so.ApplyModifiedProperties();

        EditorGUILayout.Space(8);

        var nodes = GetSelectedNodes();

        EditorGUILayout.LabelField("Selected nodes:", EditorStyles.boldLabel);
        if (nodes.Count == 0)
        {
            EditorGUILayout.HelpBox("Выдели 2 объекта с компонентом PowerNode в сцене", MessageType.Info);
        }
        else
        {
            foreach (var n in nodes)
                EditorGUILayout.LabelField($"  •  {n.name}");
        }

        EditorGUILayout.Space(8);

        bool canConnect = nodes.Count == 2 && cablePrefab != null;
        GUI.enabled = canConnect;
        if (GUILayout.Button("Connect Selected Nodes", GUILayout.Height(36)))
            ConnectNodes(nodes[0], nodes[1]);
        GUI.enabled = true;

        if (cablePrefab == null)
            EditorGUILayout.HelpBox("Назначь Cable Prefab", MessageType.Warning);
        else if (nodes.Count != 2)
            EditorGUILayout.HelpBox("Нужно выбрать ровно 2 PowerNode", MessageType.Warning);
    }

    // Перерисовываем окно при смене выделения
    private void OnSelectionChange() => Repaint();

    private List<PowerNode> GetSelectedNodes()
    {
        var result = new List<PowerNode>();
        foreach (var go in Selection.gameObjects)
        {
            var node = go.GetComponent<PowerNode>();
            if (node != null) result.Add(node);
        }
        return result;
    }

    private void ConnectNodes(PowerNode a, PowerNode b)
    {
        // Ищем общего родителя — кладём кабель туда же, где лежат ноды
        Transform parent = a.transform.parent;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(cablePrefab, parent);
        Undo.RegisterCreatedObjectUndo(go, "Connect Power Cable");

        go.name = $"Cable_{a.name}_{b.name}";
        go.transform.position = (a.transform.position + b.transform.position) * 0.5f;

        var cable = go.GetComponent<PowerCable>();
        if (cable != null)
        {
            Undo.RecordObject(cable, "Connect Power Cable");
            cable.nodeA = a;
            cable.nodeB = b;

            // Сразу выставляем LineRenderer чтобы провод был виден в редакторе
            var lr = go.GetComponent<LineRenderer>();
            if (lr == null) lr = go.GetComponentInChildren<LineRenderer>();
            if (lr != null)
            {
                Undo.RecordObject(lr, "Connect Power Cable");
                lr.positionCount = 2;
                lr.SetPosition(0, a.transform.position);
                lr.SetPosition(1, b.transform.position);
            }

            // Регистрируем кабель в нодах (для редактора — вручную)
            Undo.RecordObject(a, "Connect Power Cable");
            Undo.RecordObject(b, "Connect Power Cable");
            if (!a.connections.Contains(cable)) a.connections.Add(cable);
            if (!b.connections.Contains(cable)) b.connections.Add(cable);
        }

        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(a);
        EditorUtility.SetDirty(b);

        Selection.activeGameObject = go;
        Debug.Log($"[PowerCable] Соединено: {a.name} ↔ {b.name}");
    }
}
