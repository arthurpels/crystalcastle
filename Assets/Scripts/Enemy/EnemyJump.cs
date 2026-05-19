using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyJump : MonoBehaviour
{
    [Header("Jump Detection")]
    [SerializeField] private float maxJumpDistance = 4f;
    [SerializeField] private float maxJumpHeight = 2f;
    [SerializeField] private float minJumpHeight = 0.3f;
    [SerializeField] private float maxPathLengthForWalk = 8f; // если обход длиннее — прыгаем
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Jump Physics")]
    [SerializeField] private float jumpDuration = 0.6f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Cooldown")]
    [SerializeField] private float jumpCooldown = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private SimpleEnemyAI ai;
    private float cooldownTimer;
    private bool isJumping;
    private Vector3 jumpStart;
    private Vector3 jumpTarget;
    private float jumpProgress;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        ai = GetComponent<SimpleEnemyAI>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (isJumping)
        {
            ProcessJump();
            return;
        }

        if (ai == null || ai.CurrentState != SimpleEnemyAI.State.Chase) return;
        if (cooldownTimer > 0f) return;

        TryInitiateJump();
    }

    void TryInitiateJump()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        float horizontalDist = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(player.position.x, 0, player.position.z)
        );
        float heightDiff = player.position.y - transform.position.y;

        // Базовые проверки
        if (horizontalDist > maxJumpDistance) return;
        if (heightDiff < minJumpHeight || heightDiff > maxJumpHeight) return;

        // Проверяем путь по NavMesh
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, path);
        
        float pathLength = CalculatePathLength(path);
        bool pathIsComplete = hasPath && path.status == NavMeshPathStatus.PathComplete;
        bool pathIsShort = pathLength < maxPathLengthForWalk;

        // Если путь есть, полный и короткий — идём пешком, не прыгаем
        if (pathIsComplete && pathIsShort) return;

        // Ищем точку приземления
        if (!FindLandingSpot(player.position, out Vector3 landingPos)) return;

        // Проверяем, что приземление не дальше чем прыжок
        float landingDist = Vector3.Distance(transform.position, landingPos);
        if (landingDist > maxJumpDistance) return;

        StartJump(landingPos);
    }

    float CalculatePathLength(NavMeshPath path)
    {
        if (path == null || path.corners.Length < 2) return float.MaxValue;
        
        float length = 0f;
        for (int i = 0; i < path.corners.Length - 1; i++)
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        
        return length;
    }

    bool FindLandingSpot(Vector3 target, out Vector3 landing)
    {
        // Бросаем луч вниз от цели
        if (Physics.Raycast(target + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 5f, groundMask))
        {
            landing = hit.point;
            // Проверяем, что это валидная NavMesh-позиция
            if (NavMesh.SamplePosition(landing, out NavMeshHit navHit, 1f, NavMesh.AllAreas))
            {
                landing = navHit.position;
                return true;
            }
        }
        landing = target;
        return false;
    }

    void StartJump(Vector3 target)
    {
        isJumping = true;
        jumpStart = transform.position;
        jumpTarget = target;
        jumpProgress = 0f;

        agent.enabled = false;

        if (animator != null) animator.SetTrigger("Jump");

        Vector3 lookDir = target - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    void ProcessJump()
    {
        jumpProgress += Time.deltaTime / jumpDuration;

        if (jumpProgress >= 1f)
        {
            EndJump();
            return;
        }

        // Горизонтальное движение
        Vector3 pos = Vector3.Lerp(jumpStart, jumpTarget, jumpProgress);
        
        // Вертикальная парабола
        float height = heightCurve.Evaluate(jumpProgress) * jumpHeight;
        pos.y += height;

        transform.position = pos;
    }

    void EndJump()
    {
        isJumping = false;
        transform.position = jumpTarget;
        
        agent.enabled = true;
        agent.Warp(jumpTarget);

        cooldownTimer = jumpCooldown;

        if (animator != null) animator.SetTrigger("Land");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxJumpDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * maxJumpHeight);
    }
}