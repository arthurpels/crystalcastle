using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Мастер создания предмета в один экран. Делает все рутинные шаги:
///   1. ItemData-ассет в Assets/Data/Items
///   2. WorldItem-префаб  (_WI): mesh-ребёнок + WorldItem + BoxCollider +
///      Rigidbody(Drag2/Mass1/AngularDrag0.05/Continuous) + SaveableIdentity
///   3. HandItem-префаб   (_HI): mesh-ребёнок + скрипт-наследник HandItem
///   4. Прописывает префабы обратно в ItemData
///   5. Добавляет ItemData в ItemDatabase
///
/// Скрипт-пустышку HandItem можно сгенерировать прямо отсюда — тогда сборка
/// префабов откладывается до конца перекомпиляции ([DidReloadScripts]).
///
/// Tools → Item Creator
/// </summary>
public class ItemCreatorWindow : EditorWindow
{
    private const string ItemsFolder   = "Assets/Data/Items";
    private const string PrefabsFolder = "Assets/Prefabs/Interactive/Items";
    private const string ScriptsFolder = "Assets/Scripts/Items/HandItems";
    private const string ItemLayerName = "InteractableItems"; // слой для WorldItem (см. TagManager)

    private const string PendingKey = "ItemCreator.pending";

    // ── Поля формы ──────────────────────────────────────────────────────────
    private string     _name = "NewItem";
    private string     _displayName = "";
    private Sprite     _icon;
    private GameObject _worldMesh;
    private GameObject _handMesh;
    private int        _amount = 1;
    private bool       _stackable;
    private int        _maxStack = 1;
    private HandSlot   _allowedHand = HandSlot.Any;

    // HandItem-скрипт
    private bool     _generateScript = true;
    private Type[]   _handTypes;
    private string[] _handTypeNames;
    private int      _selectedHandType;

    [MenuItem("Tools/Item Creator")]
    private static void Open() => GetWindow<ItemCreatorWindow>("Создание предмета");

    private void OnEnable() => RefreshHandTypes();

    private void RefreshHandTypes()
    {
        _handTypes = TypeCache.GetTypesDerivedFrom<HandItem>()
            .Where(t => !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToArray();
        _handTypeNames = _handTypes.Select(t => t.Name).ToArray();
    }

    // ── GUI ───────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Создаёт ItemData + WorldItem(_WI) + HandItem(_HI) и регистрирует в ItemDatabase.\n" +
            "Коллайдер ставится по габаритам меша — подправь руками, если нужно.",
            MessageType.Info);

        EditorGUILayout.LabelField("Основное", EditorStyles.boldLabel);
        _name        = EditorGUILayout.TextField("Имя (файлы/префабы)", _name);
        _displayName = EditorGUILayout.TextField("Название (UI)", _displayName);
        _icon        = (Sprite)EditorGUILayout.ObjectField("Иконка", _icon, typeof(Sprite), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Меши", EditorStyles.boldLabel);
        _worldMesh = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Меш для мира", "Модель/префаб, который ляжет ребёнком в _WI"),
            _worldMesh, typeof(GameObject), false);
        _handMesh = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Меш для руки", "Необязательно — если пусто, берётся меш для мира"),
            _handMesh, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Параметры", EditorStyles.boldLabel);
        _amount      = EditorGUILayout.IntField("Amount (в _WI)", _amount);
        _stackable   = EditorGUILayout.Toggle("Стакается", _stackable);
        using (new EditorGUI.DisabledScope(!_stackable))
            _maxStack = EditorGUILayout.IntField("Макс. в стаке", _maxStack);
        _allowedHand = (HandSlot)EditorGUILayout.EnumPopup("Рука", _allowedHand);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("HandItem-скрипт", EditorStyles.boldLabel);
        _generateScript = EditorGUILayout.Toggle("Создать новый скрипт-пустышку", _generateScript);
        if (_generateScript)
        {
            EditorGUILayout.LabelField("Класс", $"{ScriptClassName()} : HandItem",
                EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "Скрипт сгенерируется, затем после перекомпиляции префабы соберутся сами.",
                MessageType.None);
        }
        else
        {
            if (_handTypes == null || _handTypes.Length == 0)
            {
                EditorGUILayout.HelpBox("Не найдено типов, наследующих HandItem.", MessageType.Warning);
                if (GUILayout.Button("Обновить список")) RefreshHandTypes();
            }
            else
            {
                _selectedHandType = EditorGUILayout.Popup("Тип", _selectedHandType, _handTypeNames);
                if (GUILayout.Button("Обновить список")) RefreshHandTypes();
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!CanCreate()))
        {
            if (GUILayout.Button("Создать предмет", GUILayout.Height(34)))
                CreatePressed();
        }
    }

    private bool CanCreate()
    {
        if (string.IsNullOrWhiteSpace(_name)) return false;
        if (_generateScript) return !string.IsNullOrWhiteSpace(ScriptClassName());
        return _handTypes != null && _handTypes.Length > 0;
    }

    private string SafeName() =>
        new string(_name.Trim().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

    private string ScriptClassName()
    {
        var s = SafeName();
        if (string.IsNullOrEmpty(s)) return "";
        if (char.IsDigit(s[0])) s = "_" + s;
        return s + "HandItem";
    }

    // ── Создание ────────────────────────────────────────────────────────────
    private void CreatePressed()
    {
        var payload = new PendingItem
        {
            name        = SafeName(),
            displayName = string.IsNullOrEmpty(_displayName) ? SafeName() : _displayName,
            iconGuid      = AssetGuid(_icon),
            worldMeshGuid = AssetGuid(_worldMesh),
            handMeshGuid  = AssetGuid(_handMesh != null ? _handMesh : _worldMesh),
            amount      = Mathf.Max(1, _amount),
            stackable   = _stackable,
            maxStack    = _stackable ? Mathf.Max(1, _maxStack) : 1,
            allowedHand = (int)_allowedHand,
        };

        if (_generateScript)
        {
            var className = ScriptClassName();
            var path = $"{ScriptsFolder}/{className}.cs";
            if (File.Exists(path))
            {
                if (!EditorUtility.DisplayDialog("Скрипт существует",
                        $"{path} уже есть. Использовать его и собрать префабы?", "Да", "Отмена"))
                    return;
            }
            else
            {
                EnsureFolder(ScriptsFolder);
                File.WriteAllText(path, StubScript(className));
            }

            payload.handTypeName = className;
            // Откладываем сборку до конца перекомпиляции.
            SessionState.SetString(PendingKey, JsonUtility.ToJson(payload));
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
            Debug.Log($"[ItemCreator] Скрипт {className} создан. Префабы соберутся после компиляции…");
        }
        else
        {
            payload.handTypeName = _handTypes[_selectedHandType].Name;
            BuildAll(payload);
        }
    }

    // Вызывается после перекомпиляции — доделываем отложенную сборку.
    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        var json = SessionState.GetString(PendingKey, "");
        if (string.IsNullOrEmpty(json)) return;
        SessionState.EraseString(PendingKey);

        var payload = JsonUtility.FromJson<PendingItem>(json);
        // Отложим на следующий кадр — даём ассет-базе устаканиться.
        EditorApplication.delayCall += () => BuildAll(payload);
    }

    // ── Основная сборка ───────────────────────────────────────────────────
    private static void BuildAll(PendingItem p)
    {
        var handType = TypeCache.GetTypesDerivedFrom<HandItem>()
            .FirstOrDefault(t => t.Name == p.handTypeName);
        if (handType == null)
        {
            Debug.LogError($"[ItemCreator] Тип {p.handTypeName} не найден (ошибка компиляции?). " +
                           "Исправь скрипт и запусти создание снова.");
            return;
        }

        EnsureFolder(ItemsFolder);
        EnsureFolder(PrefabsFolder);

        var icon      = LoadGuid<Sprite>(p.iconGuid);
        var worldMesh = LoadGuid<GameObject>(p.worldMeshGuid);
        var handMesh  = LoadGuid<GameObject>(p.handMeshGuid);

        // 1. ItemData ─────────────────────────────────────────────────────
        var itemPath = AssetDatabase.GenerateUniqueAssetPath($"{ItemsFolder}/{p.name}.asset");
        var item = ScriptableObject.CreateInstance<ItemData>();
        item.displayName  = p.displayName;
        item.icon         = icon;
        item.allowedHand  = (HandSlot)p.allowedHand;
        item.isStackable  = p.stackable;
        item.maxStackSize = p.maxStack;
        AssetDatabase.CreateAsset(item, itemPath);

        // 2. WorldItem (_WI) ──────────────────────────────────────────────
        var worldPrefab = BuildWorldPrefab(p, item, worldMesh);

        // 3. HandItem (_HI) ───────────────────────────────────────────────
        var handPrefab = BuildHandPrefab(p, item, handMesh, handType);

        // 4. Обратные ссылки в ItemData ───────────────────────────────────
        item.worldPrefab = worldPrefab;
        item.handPrefab  = handPrefab;
        EditorUtility.SetDirty(item);

        // 5. Регистрация в ItemDatabase ───────────────────────────────────
        RegisterInDatabase(item);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);
        Debug.Log($"[ItemCreator] Готово: «{p.name}». ItemData + _WI + _HI созданы и добавлены в базу. " +
                  "Проверь коллайдер в _WI.");
    }

    private static GameObject BuildWorldPrefab(PendingItem p, ItemData item, GameObject mesh)
    {
        var layer = LayerMask.NameToLayer(ItemLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[ItemCreator] Слой «{ItemLayerName}» не найден — ставлю Default.");
            layer = 0;
        }
        var root = new GameObject($"{p.name}_WI");
        try
        {
            AttachMesh(root, mesh);

            var box = root.AddComponent<BoxCollider>();
            FitBox(root, box);

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 2f;
            rb.angularDrag = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var wi = root.AddComponent<WorldItem>();
            wi.data   = item;
            wi.amount = p.amount;

            root.AddComponent<SaveableIdentity>();

            // Слой на всю иерархию (корень + меш-ребёнок).
            SetLayerRecursive(root, layer);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{PrefabsFolder}/{p.name}_WI.prefab");
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { DestroyImmediate(root); }
    }

    private static GameObject BuildHandPrefab(PendingItem p, ItemData item, GameObject mesh, Type handType)
    {
        var root = new GameObject($"{p.name}_HI");
        try
        {
            AttachMesh(root, mesh);

            var comp = (HandItem)root.AddComponent(handType);
            comp.data = item;

            var path = AssetDatabase.GenerateUniqueAssetPath($"{PrefabsFolder}/{p.name}_HI.prefab");
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { DestroyImmediate(root); }
    }

    // ── Хелперы ─────────────────────────────────────────────────────────────
    private static void AttachMesh(GameObject root, GameObject mesh)
    {
        if (mesh == null) return;
        var child = (GameObject)PrefabUtility.InstantiatePrefab(mesh);
        if (child == null) child = Instantiate(mesh);
        child.name = "mesh";
        // Сохраняем трансформ префаба меша (масштаб/поворот/позицию, которые ты
        // настроил) — только делаем дочерним, ничего не обнуляем.
        child.transform.SetParent(root.transform, false);
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void FitBox(GameObject root, BoxCollider box)
    {
        var rends = root.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) { box.size = Vector3.one * 0.2f; return; }

        var b = rends[0].bounds;
        foreach (var r in rends) b.Encapsulate(r.bounds);
        box.center = root.transform.InverseTransformPoint(b.center);
        box.size   = b.size;
    }

    private static void RegisterInDatabase(ItemData item)
    {
        var guids = AssetDatabase.FindAssets("t:ItemDatabase");
        if (guids.Length == 0)
        {
            Debug.LogWarning("[ItemCreator] ItemDatabase не найдена — добавь предмет в базу вручную.");
            return;
        }

        var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        var so = new SerializedObject(db);
        var list = so.FindProperty("items");

        // Не дублируем.
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item) return;

        list.InsertArrayElementAtIndex(list.arraySize);
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(db);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var leaf   = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static string AssetGuid(UnityEngine.Object o) =>
        o == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(o));

    private static T LoadGuid<T>(string guid) where T : UnityEngine.Object =>
        string.IsNullOrEmpty(guid) ? null
            : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

    private static string StubScript(string className) =>
        "using UnityEngine;\n\n" +
        $"public class {className} : HandItem {{\n" +
        "    public override void OnEquip() { }\n" +
        "    public override void OnUnequip() { }\n\n" +
        "    public override void OnUse() { }\n" +
        "}\n";

    [Serializable]
    private struct PendingItem
    {
        public string name;
        public string displayName;
        public string iconGuid;
        public string worldMeshGuid;
        public string handMeshGuid;
        public int    amount;
        public bool   stackable;
        public int    maxStack;
        public int    allowedHand;
        public string handTypeName;
    }
}
