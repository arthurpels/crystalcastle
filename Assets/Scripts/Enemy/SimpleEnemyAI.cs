using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float viewAngle = 120f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Patrol (optional)")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;

    private NavMeshAgent agent;
    private Transform player;
    private int patrolIndex;
    private float waitTimer;
    private bool isChasing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null || !enabled) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool canSee = dist < detectionRange && IsPlayerInView();

        if (canSee)
        {
            isChasing = true;
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (dist <= attackRange)
            {
                agent.isStopped = true;
                // TODO: Attack()
            }
        }
        else if (isChasing)
        {
            // Потеряли из виду — идём к последней точке
            isChasing = false;
            agent.isStopped = false;
        }
        else
        {
            Patrol();
        }
    }

    bool IsPlayerInView()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dir) < viewAngle * 0.5f;
    }

    void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.isStopped = true;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime)
            {
                waitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Vector3 left = Quaternion.Euler(0, -viewAngle/2, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle/2, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);
    }
}