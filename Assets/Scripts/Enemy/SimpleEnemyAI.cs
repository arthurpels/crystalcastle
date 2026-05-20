using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleEnemyAI : MonoBehaviour {
    public enum State { Idle, Chase, Search, Attack }

    [Header("Detection")]
    [SerializeField] private float detectionRange = 20f;
    [SerializeField] private float viewAngle = 220f;
    [SerializeField] private float memoryDuration = 8f;
    [SerializeField] private LayerMask visionBlockMask;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolWaitTime = 2f;

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 5.5f;
    [SerializeField] private float patrolSpeed = 2.5f;

    [Header("Search")]
    [SerializeField] private float searchGiveUpTime = 5f;

    // === НОВОЕ: настройки подхода к краю для прыжка ===
    [Header("Off-Mesh Approach")]
    [SerializeField] private float approachStopDistance = 0.3f; // насколько близко подойти к краю

    [Header("Direct Chase")]
    [SerializeField] private float directChaseDistance = 3f; // на какой дистанции от lastReachablePosition идём напрямую
    [SerializeField] private float directChaseSpeed = 3f;

    private NavMeshAgent agent;
    private Transform player;
    private State currentState = State.Idle;
    private Vector3 lastKnownPosition;

    private Vector3 lastReachablePosition;


    private float attackTimer;
    private int patrolIndex;
    private float waitTimer;

    private bool _playerInMemory;
    private float _memoryTimer;
    private float _searchTimer;

    // === НОВОЕ: флаг что мы идём к точке входа, а не напрямую к цели ===
    private bool _approachingEdge;

    private bool _isDirectChasing; // идём ли напрямую в обход NavMesh

    public State CurrentState => currentState;
    public Vector3 LastKnownPosition => lastKnownPosition;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update() {
        if (player == null) return;

        // Пока агент проходит off-mesh связь — FSM на паузе
        if (agent.isOnOffMeshLink) return;

        if (_isDirectChasing) {
            DirectChaseTick();
            return;
        }

        UpdateMemory();

        switch (currentState) {
            case State.Idle: TickIdle(); break;
            case State.Chase: TickChase(); break;
            case State.Search: TickSearch(); break;
            case State.Attack: TickAttack(); break;
        }
    }

    private void UpdateMemory() {
        if (CanSeePlayer()) {
            _playerInMemory = true;
            _memoryTimer = memoryDuration;
            lastKnownPosition = player.position;

            // === НОВОЕ: если можем построить путь — запоминаем как "доступную" ===
            if (HasPathTo(player.position)) {
                lastReachablePosition = player.position;
                _isDirectChasing = false; // сбрасываем, путь есть
            }
        } else if (_playerInMemory) {
            _memoryTimer -= Time.deltaTime;
            if (_memoryTimer <= 0f) _playerInMemory = false;
        }
    }

    private void TickIdle() {
        agent.speed = patrolSpeed;
        if (_playerInMemory) { EnterChase(); return; }
        Patrol();
    }

    private void TickChase() {
        agent.speed = chaseSpeed;

        if (!_playerInMemory) { EnterSearch(); return; }

        // === ИЗМЕНЕНО: идём к последней ДОСТИЖИМОЙ позиции, не к текущей ===
        float distToReachable = Vector3.Distance(transform.position, lastReachablePosition);
        bool canPathToPlayer = HasPathTo(player.position);

        if (distToReachable < directChaseDistance && !canPathToPlayer && CanSeePlayer()) {
            // Дошли до края, видим игрока, но пути нет — идём напрямую
            StartDirectChase();
            return;
        }

        // Обычный chase по NavMesh
        Vector3 target = canPathToPlayer ? player.position : lastReachablePosition;
        if (canPathToPlayer) lastReachablePosition = player.position;


        agent.isStopped = false;
        agent.SetDestination(target);

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange) {
            agent.isStopped = true;
            currentState = State.Attack;
        }
    }

    private void TickSearch() {
        agent.speed = patrolSpeed * 1.2f;
        agent.isStopped = false;

        // === ИЗМЕНЕНО: идём к последней достижимой позиции ===
        agent.SetDestination(lastReachablePosition);

        if (_playerInMemory) { EnterChase(); return; }

        _searchTimer += Time.deltaTime;
        if (_searchTimer > searchGiveUpTime) { currentState = State.Idle; return; }

        // Дошли до точки — постояли, сдались
        if (!agent.pathPending && agent.remainingDistance < 1f) {
            waitTimer += Time.deltaTime;
            if (waitTimer > 2f) currentState = State.Idle;
        }
    }

    // === НОВЫЙ МЕТОД ===
    private bool HasPathTo(Vector3 target) {
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path);
        return hasPath && path.status == NavMeshPathStatus.PathComplete;
    }

    private void TickAttack() {
        attackTimer -= Time.deltaTime;
        LookAtPlayer();

        if (attackTimer <= 0f) {
            Debug.Log("АТАКА!");
            attackTimer = attackCooldown;
        }

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackRange + 0.5f) {
            agent.isStopped = false;
            currentState = State.Chase;
        } else if (!_playerInMemory) {
            agent.isStopped = false;
            currentState = State.Search;
        }
    }

    private void StartDirectChase() {
        _isDirectChasing = true;
        agent.isStopped = true; // NavMeshAgent не мешает
        Debug.Log("[AI] Прямое преследование!");
    }

    private void DirectChaseTick() {
        if (!_playerInMemory || !CanSeePlayer()) {
            // Потеряли игрока — возвращаемся к NavMesh
            _isDirectChasing = false;
            agent.isStopped = false;
            EnterSearch();
            return;
        }

        // Идём напрямую к игроку, игнорируя NavMesh
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0; // не летаем

        transform.position += dir * directChaseSpeed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(dir);

        // Проверяем, не появился ли путь (спрыгнул/доступен)
        if (HasPathTo(player.position)) {
            _isDirectChasing = false;
            lastReachablePosition = player.position;
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        // Проверяем дистанцию атаки
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange) {
            _isDirectChasing = false;
            currentState = State.Attack;
        }
    }

    // === НОВЫЕ МЕТОДЫ ===

    /// <summary>
    /// Проверяет, есть ли полный путь по NavMesh до точки (без разрывов)
    /// </summary>
    private bool HasCompletePathTo(Vector3 target) {
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path);
        return hasPath && path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Находит ближайшую точку на краю текущего NavMesh-острова к цели.
    /// Агент пойдёт сюда, и DynamicNavMeshLink создаст связь отсюда к целевому острову.
    /// </summary>
    private Vector3 FindNearestNavMeshEdge(Vector3 target) {
        // Строим путь — он дойдёт до ближайшей точки на нашем острове
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path);

        if (hasPath && path.corners.Length > 0) {
            // Последняя достижимая точка — край нашего острова
            Vector3 edgePoint = path.corners[path.corners.Length - 1];

            // Если это уже цель — путь полный, не нужен край
            if (Vector3.Distance(edgePoint, target) < 0.5f)
                return Vector3.zero;

            return edgePoint;
        }

        // Fallback: ищем ближайшую точку на NavMesh к цели
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 10f, NavMesh.AllAreas)) {
            // Проверяем, что это на другом острове (выше или дальше)
            if (Mathf.Abs(hit.position.y - transform.position.y) > 0.5f)
                return hit.position;
        }

        return Vector3.zero;
    }

    private void EnterChase() {
        currentState = State.Chase;
        agent.isStopped = false;
        _searchTimer = 0f;
        _approachingEdge = false;
    }

    private void EnterSearch() {
        currentState = State.Search;
        waitTimer = 0f;
        _searchTimer = 0f;
        _approachingEdge = false;
    }

    private bool CanSeePlayer() {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        if (Vector3.Angle(transform.forward, dirToPlayer) > viewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 rayDir = targetPos - eyePos;

        if (Physics.Raycast(eyePos, rayDir.normalized, out RaycastHit hit, detectionRange,
                            visionBlockMask, QueryTriggerInteraction.Ignore)) {
            if (!hit.transform.CompareTag("Player")) return false;
        }
        return true;
    }

    private void LookAtPlayer() {
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);
    }

    private void Patrol() {
        if (patrolPoints == null || patrolPoints.Length == 0) {
            agent.isStopped = true;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < 0.5f) {
            waitTimer += Time.deltaTime;
            if (waitTimer >= patrolWaitTime) {
                waitTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[patrolIndex].position);
                agent.isStopped = false;
            } else {
                agent.isStopped = true;
            }
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);

        if (currentState == State.Search || currentState == State.Chase) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
        }

        // === НОВОЕ: рисуем точку края, куда идём ===
        if (_approachingEdge) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(agent.destination, 0.2f);
            Gizmos.DrawLine(transform.position, agent.destination);
        }
    }
}