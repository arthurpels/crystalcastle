using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Реестр всех ItemData в проекте. Нужен системе сохранений для поиска
/// предметов по имени ассета при загрузке инвентаря.
///
/// Настройка:
///   1. Assets → Create → CrystalCastle → Item Database
///   2. Перетащи все ItemData-ассеты в список Items.
///   3. Назначь ассет в поле SaveManager.itemDatabase.
/// </summary>
[CreateAssetMenu(menuName = "CrystalCastle/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> items = new List<ItemData>();

    private Dictionary<string, ItemData> _cache;

    /// <summary>Найти ItemData по имени ассета (item.name, то есть имя файла без .asset).</summary>
    public ItemData Find(string assetName)
    {
        if (_cache == null) BuildCache();
        return _cache.TryGetValue(assetName, out var result) ? result : null;
    }

    private void BuildCache()
    {
        _cache = new Dictionary<string, ItemData>();
        foreach (var item in items)
            if (item != null && !_cache.ContainsKey(item.name))
                _cache[item.name] = item;
    }

    // При изменении списка в редакторе — сбросить кеш.
    private void OnValidate() => _cache = null;
}
