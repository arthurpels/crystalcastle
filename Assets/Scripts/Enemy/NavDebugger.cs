using UnityEngine;
using UnityEngine.AI;

public class NavDebug : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) return;

        // Проверяем, может ли NavMesh построить путь
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, path);

        Debug.DrawLine(transform.position, player.position, hasPath ? Color.green : Color.red);

        if (agent.hasPath)
        {
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Debug.DrawLine(corners[i], corners[i + 1], Color.yellow);
        }

        // Статус пути
        string status = hasPath ? path.status.ToString() : "NO PATH";
        Debug.Log($"[NavDebug] Path: {status}, Agent pathPending: {agent.pathPending}, remaining: {agent.remainingDistance}, isOnOffMesh: {agent.isOnOffMeshLink}");
    }
}