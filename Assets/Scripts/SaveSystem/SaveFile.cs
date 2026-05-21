using System;
using System.Collections.Generic;

/// <summary>
/// Корневой объект файла сохранения. Сериализуется в JSON через JsonUtility.
/// </summary>
[Serializable]
public class SaveFile
{
    public int    version  = 2;
    public string saveTime;   // "21.05.2026 14:30"
    public string sceneName;  // SceneManager.GetActiveScene().name

    // Состояние игрока
    public PlayerSaveData player = new PlayerSaveData();

    // Состояние всех ISaveable-объектов в сцене (живых)
    public List<ObjectEntry> objects = new List<ObjectEntry>();

    // Уничтоженные объекты (убитые враги, поднятые предметы, сломанные барьеры)
    // При загрузке эти объекты Destroy-ятся сразу после появления сцены.
    public List<string> destroyedIds = new List<string>();
}

[Serializable]
public class PlayerSaveData
{
    // Позиция и поворот
    public float px, py, pz;
    public float ry;            // rotation Y (евлер)

    // Здоровье и стамина
    public float hp;
    public float stamina;

    // Инвентарь — имя ассета + количество в стаке
    public List<InventoryItemEntry> inventoryItems = new List<InventoryItemEntry>();

    // Что экипировано в руки (asset.name или "" если пусто)
    public string leftHandItem;
    public string rightHandItem;
}

[Serializable]
public class ObjectEntry
{
    public string id;    // SaveableIdentity.Id
    public string data;  // JsonUtility.ToJson(...) конкретного типа
}

[Serializable]
public class InventoryItemEntry
{
    public string itemName; // ItemData.name (имя ассета)
    public int    count;    // количество в стаке
}
