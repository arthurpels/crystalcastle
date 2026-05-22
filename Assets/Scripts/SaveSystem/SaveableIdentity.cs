using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

/// <summary>
/// Стабильный GUID для объектов в сцене.
/// Префаб-ассет в Project хранит пустой ID — инстанс получает его при появлении в сцене.
/// При копировании (Ctrl+D) или drag из префаба ID перегенерируется автоматически.
/// </summary>
[DisallowMultipleComponent]
public class SaveableIdentity : MonoBehaviour
{
    [SerializeField, HideInInspector] private string _id;
    public string Id => _id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ── 1. Ассет-префаб в Project: ID должен быть пустым ─────────────
        if (PrefabUtility.IsPartOfPrefabAsset(this))
        {
            if (!string.IsNullOrEmpty(_id))
            {
                _id = null;
                EditorUtility.SetDirty(this);
            }
            return;
        }

        // ── 2. Новый объект в сцене (drag префаба, Ctrl+D, создание вручную)
        // Если пусто — генерируем новый GUID.
        if (string.IsNullOrEmpty(_id))
        {
            _id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            return;
        }

        // ── 3. Проверка дублей только среди объектов В СЦЕНЕ (не ассеты)
        // Это отловит Ctrl+D и случайные совпадения.
        var allInScene = FindObjectsOfType<SaveableIdentity>(true)
            .Where(x => !PrefabUtility.IsPartOfPrefabAsset(x))
            .ToArray();

        int duplicates = 0;
        foreach (var other in allInScene)
        {
            if (other == this) continue;
            if (other._id == _id) duplicates++;
        }

        if (duplicates > 0)
        {
            string oldId = _id;
            _id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(this);
            Debug.LogWarning(
                $"[SaveableIdentity] Дубль ID '{oldId}' на '{gameObject.name}'. " +
                $"Новый ID: {_id}. Причина: Ctrl+D или drag одинакового префаба.", this);
        }
    }

    // ── Инструменты для меню ─────────────────────────────────────────────

    [ContextMenu("Save System/Regenerate ID")]
    private void RegenerateIdContext()
    {
        _id = System.Guid.NewGuid().ToString();
        EditorUtility.SetDirty(this);
        Debug.Log($"[SaveableIdentity] Новый ID для '{name}': {_id}", this);
    }

    [MenuItem("Tools/Save System/Regenerate All IDs in Open Scenes")]
    private static void RegenerateAllIdsInOpenScenes()
    {
        var all = FindObjectsOfType<SaveableIdentity>(true)
            .Where(x => !PrefabUtility.IsPartOfPrefabAsset(x))
            .ToArray();

        int count = 0;
        foreach (var si in all)
        {
            si._id = System.Guid.NewGuid().ToString();
            EditorUtility.SetDirty(si);
            count++;
        }

        Debug.Log($"[SaveableIdentity] Перегенерировано {count} ID в открытых сценах.");

        // Принудительно сохраняем сцену, чтобы не потерять
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Tools/Save System/Clear IDs inside Prefab Assets (Project)")]
    private static void ClearIdsInPrefabAssets()
    {
        // Ищем все SaveableIdentity в AssetDatabase
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int cleared = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabRoot == null) continue;

            var identities = prefabRoot.GetComponentsInChildren<SaveableIdentity>(true);
            foreach (var si in identities)
            {
                if (!string.IsNullOrEmpty(si._id))
                {
                    si._id = null;
                    EditorUtility.SetDirty(si);
                    cleared++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[SaveableIdentity] Очищено {cleared} ID внутри префабов-ассетов.");
    }
#endif

    // ── Runtime (билд) ───────────────────────────────────────────────────
    private void Awake()
    {
        if (string.IsNullOrEmpty(_id))
            _id = System.Guid.NewGuid().ToString();
    }
}