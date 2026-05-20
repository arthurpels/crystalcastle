using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class OffMeshLinkGenerator : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float scanRadius = 50f;
    [SerializeField] private float maxJumpHeight = 2.5f;
    [SerializeField] private float maxJumpDistance = 3f;
    [SerializeField] private float minHeightDiff = 0.3f;

    [Header("Link Settings")]
    [SerializeField] private float linkWidth = 0.5f;
    [SerializeField] private int maxLinks = 20;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;

    void Start()
    {
        GenerateJumpLinks();
    }

    [ContextMenu("Generate Jump Links")]
    public void GenerateJumpLinks()
    {
        // Удаляем старые ссылки, созданные этим генератором
        var oldLinks = GetComponentsInChildren<OffMeshLink>();
        foreach (var link in oldLinks)
        {
            if (link.name.StartsWith("AutoJumpLink"))
                Destroy(link.gameObject);
        }

        // Собираем все точки NavMesh в радиусе
        List<Vector3> meshPoints = new();
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        for (int i = 0; i < triangulation.vertices.Length; i++)
        {
            if (Vector3.Distance(transform.position, triangulation.vertices[i]) < scanRadius)
                meshPoints.Add(triangulation.vertices[i]);
        }

        int created = 0;

        // Ищем пары точек: одна ниже, другая выше, в пределах дистанции
        for (int i = 0; i < meshPoints.Count && created < maxLinks; i++)
        {
            for (int j = i + 1; j < meshPoints.Count && created < maxLinks; j++)
            {
                Vector3 lower = meshPoints[i];
                Vector3 higher = meshPoints[j];
                float heightDiff = higher.y - lower.y;

                // Проверяем направление (только вверх от lower к higher)
                if (heightDiff < minHeightDiff || heightDiff > maxJumpHeight)
                    continue;

                float horizontalDist = Vector3.Distance(
                    new Vector3(lower.x, 0, lower.z),
                    new Vector3(higher.x, 0, higher.z)
                );

                if (horizontalDist > maxJumpDistance)
                    continue;

                // Проверяем, что между точками нет стены (Raycast)
                Vector3 midPoint = Vector3.Lerp(lower, higher, 0.5f);
                if (Physics.Raycast(midPoint, Vector3.up, heightDiff * 0.5f))
                    continue; // Препятствие над точкой

                // Создаём ссылку
                CreateBidirectionalLink(lower, higher);
                created++;
            }
        }

        Debug.Log($"[OffMeshLinkGenerator] Создано {created} jump-ссылок");
    }

    void CreateBidirectionalLink(Vector3 start, Vector3 end)
    {
        GameObject linkObj = new GameObject($"AutoJumpLink_{start.GetHashCode()}");
        linkObj.transform.SetParent(transform);

        // Создаём точки входа/выхода
        GameObject startPoint = new GameObject("Start");
        startPoint.transform.SetParent(linkObj.transform);
        startPoint.transform.position = start + Vector3.up * 0.1f; // чуть выше NavMesh

        GameObject endPoint = new GameObject("End");
        endPoint.transform.SetParent(linkObj.transform);
        endPoint.transform.position = end + Vector3.up * 0.1f;

        // Компонент OffMeshLink
        OffMeshLink link = linkObj.AddComponent<OffMeshLink>();
        link.startTransform = startPoint.transform;
        link.endTransform = endPoint.transform;
        link.biDirectional = true;
        link.activated = true;
        link.costOverride = 2f; // дороже обычного пути
        link.area = 0; // Walkable
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, scanRadius);

        // Рисуем существующие ссылки
        var links = GetComponentsInChildren<OffMeshLink>();
        foreach (var link in links)
        {
            if (link.startTransform == null || link.endTransform == null) continue;

            Gizmos.color = link.activated ? Color.cyan : Color.gray;
            Gizmos.DrawLine(link.startTransform.position, link.endTransform.position);
            Gizmos.DrawSphere(link.startTransform.position, 0.1f);
            Gizmos.DrawSphere(link.endTransform.position, 0.1f);
        }
    }
}