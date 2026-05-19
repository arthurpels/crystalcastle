using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Базовый ИИ врага: патруль / преследование / поиск / атака.
/// Прыжки между разорванными островами NavMesh выполняются автоматически:
/// агент строит маршрут через NavMeshLink сам, траверс ведёт OffMeshLinkJump.
/// </summary>
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

    private NavMeshAgent agent;
    private Transform player;
    private State currentState = State.Idle;
    private Vector3 lastKnownPosition;

    private float attackTimer;
    private int patrolIndex;
    private float waitTimer;

    private bool _playerInMemory;
    private float _memoryTimer;
    private float _searchTimer;

    public State CurrentState => currentState;
    public Vector3 LastKnownPosition => lastKnownPosition;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update() {
        if (player == null) return;

        // Пока агент проходит off-mesh связь (NavMeshLink) — траверс ведёт
        // OffMeshLinkJump, FSM ставится на паузу до завершения прыжка.
        if (agent.isOnOffMeshLink) return;

        UpdateMemory();

        switch (currentState) {
            case State.Idle:   TickIdle();   break;
            case State.Chase:  TickChase();  break;
            case State.Search: TickSearch(); break;
            case State.Attack: TickAttack(); break;
        }
    }

    // --- Память: враг помнит позицию игрока memoryDuration секунд ---
    private void UpdateMemory() {
        if (CanSeePlayer()) {
            _playerInMemory = true;
            _memoryTimer = memoryDuration;
            lastKnownPosition = player.position;
        } else if (_playerInMemory) {
            _memoryTimer -= Time.deltaTime;
            if (_memoryTimer <= 0f) _playerInMemory = false;
        }
    }

    // --- Состояния ---

    private void TickIdle() {
        agent.speed = patrolSpeed;
        if (_playerInMemory) { EnterChase(); return; }
        Patrol();
    }

    private void TickChase() {
        agent.speed = chaseSpeed;

        if (!_playerInMemory) { EnterSearch(); return; }

        // Unity сама построит маршрут через NavMeshLink, если он есть
        agent.isStopped = false;
        agent.SetDestination(player.position);

        if (Vector3.Distance(transform.position, player.position) <= attackRange) {
            agent.isStopped = true;
            currentState = State.Attack;
        }
    }

    private void TickSearch() {
        agent.speed = patrolSpeed * 1.2f;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPosition);

        if (_playerInMemory) { EnterChase(); return; }

        _searchTimer += Time.deltaTime;
        if (_searchTimer > searchGiveUpTime) { currentState = State.Idle; return; }

        // Дошёл до последней известной точки — постоял и сдался
        if (!agent.pathPending
            && agent.remainingDistance < 1f
            && agent.pathStatus == NavMeshPathStatus.PathComplete) {
            waitTimer += Time.deltaTime;
            if (waitTimer > 2f) currentState = State.Idle;
        }
    }

    private void TickAttack() {
        attackTimer -= Time.deltaTime;
        LookAtPlayer();

        if (attackTimer <= 0f) {
            Debug.Log("АТАКА!"); // TODO: нанести урон игроку
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

    // --- Переходы ---

    private void EnterChase() {
        currentState = State.Chase;
        agent.isStopped = false;
        _searchTimer = 0f;
    }

    private void EnterSearch() {
        currentState = State.Search;
        waitTimer = 0f;
        _searchTimer = 0f;
    }

    // --- Утилиты ---

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
        Vector3 left  = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0,  viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left  * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);

        if (currentState == State.Search || currentState == State.Chase) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
        }
    }
}
