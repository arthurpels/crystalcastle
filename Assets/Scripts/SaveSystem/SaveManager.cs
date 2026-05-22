using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Синглтон. DontDestroyOnLoad.
///
/// === Как использовать ===
///   // Сохранить в слот 0:
///   SaveManager.Instance.Save(0);
///
///   // Загрузить из слота 0:
///   SaveManager.Instance.Load(0);
///
///   // Узнать есть ли сохранение:
///   SaveManager.Instance.SlotExists(0);
///
///   // Получить метаданные слота (для UI):
///   SaveFile meta = SaveManager.Instance.PeekSlot(0);
///
///   // Уведомить о уничтожении (из EnemyHealth, WorldItem, DestructibleObject):
///   SaveManager.Instance?.RegisterDestroyed(this);
///
/// === Как добавить новый сохраняемый объект ===
///   1. Добавь SaveableIdentity на GameObject.
///   2. Реализуй ISaveable на компоненте (CaptureState / RestoreState).
///   3. Готово — SaveManager найдёт его автоматически.
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static SaveManager Instance { get; private set; }

    // ── Settings ──────────────────────────────────────────────────────────
    public const int SlotCount = 3;

    [Header("Ссылки")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Дебаг")]
    [SerializeField] private bool verboseLog = true;

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Вызывается после полного применения данных загрузки.</summary>
    public static event Action OnAfterLoad;

    // ── Runtime state ─────────────────────────────────────────────────────
    /// <summary>
    /// ID объектов, уничтоженных во время текущей игровой сессии.
    /// Заполняется через RegisterDestroyed().
    /// </summary>
    private readonly HashSet<string> _destroyedIds = new HashSet<string>();

    /// <summary>Данные для загрузки, ожидающие применения после смены сцены.</summary>
    private SaveFile _pendingLoad;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Сохранить текущее состояние в указанный слот.</summary>
    public void Save(int slot)
    {
        var file = new SaveFile
        {
            saveTime  = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            sceneName = SceneManager.GetActiveScene().name,
            destroyedIds = new List<string>(_destroyedIds)
        };

        // ── Объекты сцены (ISaveable + SaveableIdentity) ───────────────
        foreach (var identity in FindObjectsOfType<SaveableIdentity>(true))
        {
            var saveable = identity.GetComponent<ISaveable>();
            if (saveable == null) continue;

            // RuntimeSpawned WorldItems сохраняем в spawnedItems, не здесь.
            var wi = identity.GetComponent<WorldItem>();
            if (wi != null && wi.isRuntimeSpawned) continue;

            file.objects.Add(new ObjectEntry
            {
                id   = identity.Id,
                data = saveable.CaptureState()
            });
        }

        // ── Runtime WorldItems (дропнутые / из ящиков) ────────────────
        foreach (var wi in FindObjectsOfType<WorldItem>(true))
        {
            if (!wi.isRuntimeSpawned) continue;
            if (wi.data == null) continue;

            var pos = wi.transform.position;
            file.spawnedItems.Add(new SpawnedItemEntry
            {
                itemName = wi.data.name,
                amount   = wi.amount,
                px = pos.x, py = pos.y, pz = pos.z,
                ry = wi.transform.eulerAngles.y
            });
        }

        // ── Игрок ──────────────────────────────────────────────────────
        var playerAttribs = FindObjectOfType<PlayerAttributes>();
        var playerInv     = FindObjectOfType<PlayerInventory>();
        if (playerAttribs != null)
            file.player = CapturePlayer(playerAttribs, playerInv);

        WriteSlot(slot, file);
        Log($"[SaveManager] Сохранено в слот {slot}: {file.objects.Count} объектов, " +
            $"{file.spawnedItems.Count} runtime-предметов, " +
            $"{file.destroyedIds.Count} уничтожено.");
    }

    /// <summary>Загрузить из слота: перезагружает сцену, затем применяет данные.</summary>
    public void Load(int slot)
    {
        var file = ReadSlot(slot);
        if (file == null) { Debug.LogWarning($"[SaveManager] Слот {slot} пуст."); return; }

        _pendingLoad = file;
        _destroyedIds.Clear(); // сброс — загруженные destroyedIds придут из файла
        SceneManager.LoadScene(file.sceneName);
    }

    /// <summary>Удалить сохранение из слота.</summary>
    public void DeleteSlot(int slot)
    {
        var path = GetPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <returns>true, если в слоте есть сохранение.</returns>
    public bool SlotExists(int slot) => File.Exists(GetPath(slot));

    /// <summary>Прочитать метаданные слота без загрузки сцены (для UI).</summary>
    public SaveFile PeekSlot(int slot) => SlotExists(slot) ? ReadSlot(slot) : null;

    /// <summary>
    /// Вызывай из компонентов ПЕРЕД Destroy(gameObject),
    /// чтобы SaveManager знал, что объект был уничтожен.
    /// </summary>
    public void RegisterDestroyed(MonoBehaviour source)
    {
        var identity = source.GetComponent<SaveableIdentity>();
        if (identity == null || string.IsNullOrEmpty(identity.Id)) return;
        _destroyedIds.Add(identity.Id);
        Log($"[SaveManager] RegisterDestroyed: {source.name} ({identity.Id})");
    }

    // ── Scene loaded ──────────────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_pendingLoad == null) return;
        var data = _pendingLoad;
        _pendingLoad = null;
        StartCoroutine(ApplySaveFile(data));
    }

    // ── Apply save file ───────────────────────────────────────────────────

    private IEnumerator ApplySaveFile(SaveFile file)
    {
        // Один кадр — ждём Awake/Start всех объектов сцены.
        yield return null;

        Log($"[SaveManager] Применяю сохранение: {file.objects.Count} объектов, " +
            $"{file.destroyedIds.Count} уничтожено.");

        // Сброс архива документов перед восстановлением
        if (DocumentManager.Instance != null)
            DocumentManager.Instance.ResetForLoad();

        // Строим словарь id → (identity, saveable) для быстрого поиска.
        var map = new Dictionary<string, (SaveableIdentity id, ISaveable s)>();
        foreach (var identity in FindObjectsOfType<SaveableIdentity>(true))
        {
            var saveable = identity.GetComponent<ISaveable>();
            if (saveable != null)
                map[identity.Id] = (identity, saveable);
        }

        // 1. Уничтожаем объекты, удалённые в сохранённой сессии.
        var destroyedSet = new HashSet<string>(file.destroyedIds);
        foreach (var id in destroyedSet)
        {
            if (map.TryGetValue(id, out var entry))
            {
                Log($"[SaveManager] Уничтожаю (был удалён): {entry.id.name}");
                Destroy(entry.id.gameObject);
            }
        }
        _destroyedIds.UnionWith(destroyedSet); // восстанавливаем в памяти

        // 2. Восстанавливаем живые объекты.
        foreach (var entry in file.objects)
        {
            if (destroyedSet.Contains(entry.id)) continue;
            if (!map.TryGetValue(entry.id, out var obj))
            {
                Log($"[SaveManager] Объект не найден: {entry.id}");
                continue;
            }
            obj.s.RestoreState(entry.data);
        }

        // 3. Игрок.
        RestorePlayer(file.player);

        // 4. Respawn runtime WorldItems (дропнутые / из ящиков).
        foreach (var entry in file.spawnedItems)
        {
            if (itemDatabase == null) break;
            var itemData = itemDatabase.Find(entry.itemName);
            if (itemData == null || itemData.worldPrefab == null)
            {
                Log($"[SaveManager] WorldItem не найден: '{entry.itemName}'");
                continue;
            }
            var go = Instantiate(
                itemData.worldPrefab,
                new Vector3(entry.px, entry.py, entry.pz),
                Quaternion.Euler(0f, entry.ry, 0f));

            var wi = go.GetComponent<WorldItem>();
            if (wi != null)
            {
                wi.amount           = entry.amount;
                wi.isRuntimeSpawned = true;
            }
        }

        // Ещё один кадр перед пересчётом сети (чтобы Start() объектов отработал).
        yield return null;

        // 5. Пересчитываем сеть питания.
        if (PowerNetwork.Instance != null)
            PowerNetwork.Instance.Evaluate();

        OnAfterLoad?.Invoke();
        Log("[SaveManager] Загрузка завершена.");
    }

    // ── Player capture / restore ──────────────────────────────────────────

    private PlayerSaveData CapturePlayer(PlayerAttributes attribs, PlayerInventory inv)
    {
        var data = new PlayerSaveData
        {
            px      = attribs.transform.position.x,
            py      = attribs.transform.position.y,
            pz      = attribs.transform.position.z,
            ry      = attribs.transform.eulerAngles.y,
            hp      = attribs.CurrentHP,
            stamina = attribs.CurrentStamina
        };

        if (inv != null)
        {
            data.inventoryItems = inv.inventory
                .Where(i => i != null && i.itemData != null)
                .Select(i => new InventoryItemEntry { itemName = i.itemData.name, count = i.count })
                .ToList();

            // ItemSlot — MonoBehaviour, используем явные null-проверки
            if (inv.leftHandSlot != null && inv.leftHandSlot.CurrentItem != null
                && inv.leftHandSlot.CurrentItem.itemData != null)
                data.leftHandItem = inv.leftHandSlot.CurrentItem.itemData.name;
            else
                data.leftHandItem = "";

            if (inv.rightHandSlot != null && inv.rightHandSlot.CurrentItem != null
                && inv.rightHandSlot.CurrentItem.itemData != null)
                data.rightHandItem = inv.rightHandSlot.CurrentItem.itemData.name;
            else
                data.rightHandItem = "";
        }

        return data;
    }

    private void RestorePlayer(PlayerSaveData data)
    {
        var attribs = FindObjectOfType<PlayerAttributes>();
        var inv     = FindObjectOfType<PlayerInventory>();

        if (attribs == null) { Debug.LogWarning("[SaveManager] PlayerAttributes не найден!"); return; }

        // ── Позиция ────────────────────────────────────────────────────
        // CharacterController нужно отключить перед телепортацией.
        var cc = attribs.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        attribs.transform.SetPositionAndRotation(
            new Vector3(data.px, data.py, data.pz),
            Quaternion.Euler(0f, data.ry, 0f));
        if (cc != null) cc.enabled = true;

        // ── Здоровье / стамина ────────────────────────────────────────
        // RestoreForLoad: прямая установка без звука урона / вспышки вигнетки.
        attribs.RestoreForLoad(data.hp, data.stamina);

        // ── Инвентарь ──────────────────────────────────────────────────
        if (inv == null || itemDatabase == null) return;

        inv.ClearForLoad();

        InventoryItem leftItem  = null;
        InventoryItem rightItem = null;

        foreach (var entry in data.inventoryItems)
        {
            var itemData = itemDatabase.Find(entry.itemName);
            if (itemData == null)
            {
                Debug.LogWarning($"[SaveManager] ItemData не найдена: '{entry.itemName}'");
                continue;
            }
            var added = inv.Add(itemData, entry.count);
            if (added == null) continue;

            if (!string.IsNullOrEmpty(data.leftHandItem)  && entry.itemName == data.leftHandItem)  leftItem  = added;
            if (!string.IsNullOrEmpty(data.rightHandItem) && entry.itemName == data.rightHandItem) rightItem = added;
        }

        if (leftItem  != null) inv.Equip(leftItem,  inv.leftHandSlot);
        if (rightItem != null) inv.Equip(rightItem, inv.rightHandSlot);
    }

    // ── File I/O ──────────────────────────────────────────────────────────

    private static string GetPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"cc_save_{slot}.json");

    private static void WriteSlot(int slot, SaveFile file) =>
        File.WriteAllText(GetPath(slot), JsonUtility.ToJson(file, prettyPrint: true));

    private static SaveFile ReadSlot(int slot)
    {
        var path = GetPath(slot);
        if (!File.Exists(path)) return null;
        try   { return JsonUtility.FromJson<SaveFile>(File.ReadAllText(path)); }
        catch { Debug.LogError($"[SaveManager] Ошибка чтения слота {slot}."); return null; }
    }

    // ── Utils ─────────────────────────────────────────────────────────────

    private void Log(string msg) { if (verboseLog) Debug.Log(msg); }
}
