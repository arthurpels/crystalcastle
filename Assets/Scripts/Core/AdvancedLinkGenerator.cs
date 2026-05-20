using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;

public class AdvancedLinkGenerator : EditorWindow
{
    [MenuItem("Tools/Сгенерировать прыжки ВВЕРХ и ВНИЗ")]
    public static void GenerateLinks()
    {
        // Находим все объекты с тегом "Obstacle"
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Ground");
        int linkCount = 0;

        foreach (GameObject obstacle in obstacles)
        {
            Collider col = obstacle.GetComponent<Collider>();
            if (col == null) continue;

            Bounds bounds = col.bounds;
            
            // Удаляем старые авто-линки на этом объекте, если они были сгенерированы ранее
            foreach (Transform child in obstacle.transform) {
                if (child.name.StartsWith("AutoSide_")) {
                    DestroyImmediate(child.gameObject);
                }
            }

            // Направления для генерации линков (Вперед, Назад, Вправо, Влево)
            Vector3[] directions = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

            foreach (Vector3 dir in directions)
            {
                GameObject linkObj = new GameObject("AutoSide_" + dir.ToString() + "_" + obstacle.name);
                linkObj.transform.parent = obstacle.transform;
                linkObj.transform.localPosition = Vector3.zero;

                NavMeshLink link = linkObj.AddComponent<NavMeshLink>();
                
                // --- ВОТ ТУТ МАГИЯ ДВУСТОРОННОСТИ ---
                link.bidirectional = true; // Теперь агент официально может ходить и ВВЕРХ, и ВНИЗ
                
                // Смещение от центра объекта в зависимости от направления
                float offsetX = dir.x * (bounds.extents.x + 0.4f);
                float offsetZ = dir.z * (bounds.extents.z + 0.4f);

                // Точка СТАРТА (на земле перед препятствием)
                link.startPoint = new Vector3(offsetX, -bounds.extents.y, offsetZ);
                
                // Точка ФИНИША (на краю крыши препятствия)
                // Сдвигаем чуть внутрь (на 0.2f меньше), чтобы враг гарантированно приземлялся НА ящик, а не падал с края
                float insideX = dir.x * (bounds.extents.x - 0.2f);
                float insideZ = dir.z * (bounds.extents.z - 0.2f);
                link.endPoint = new Vector3(insideX, bounds.extents.y, insideZ);

                linkCount++;
            }
        }

        Debug.Log($"Готово! Создано {linkCount} честных двусторонних линков для запрыгивания.");
    }
}