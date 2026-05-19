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

    [Header("Search Settings")]
    [SerializeField] private float stuckThreshold = 1.5f;
    [SerializeField] private float searchGiveUpTime = 5f;

    [Header("OffMesh Navigation")]
    [SerializeField] private float offMeshReachDistance = 2f; // насколько близко подойти к краю

    private NavMeshAgent agent;
    private Transform player;
    private State currentState = State.Idle;
    private Vector3 lastKnownPosition;
    private float attackTimer;
    private int patrolIndex;
    private float waitTimer;

    private bool _playerInMemory;
    private float _memoryTimer;
    private Vector3 _lastPosition;
    private float _stuckTimer;
    private float _searchTimer;
    private bool _goingToOffMesh; // идём к точке входа на OffMeshLink

    public State CurrentState => currentState;
    public Vector3 LastKnownPosition => lastKnownPosition;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update() {
        if (player == null) return;

        bool seesNow = CanSeePlayer();

        // --- Система памяти ---
        if (seesNow) {
            _playerInMemory = true;
            _memoryTimer = memoryDuration;
            lastKnownPosition = player.position;
            _goingToOffMesh = false; // сбрасываем, игрок виден напрямую
        } else if (_playerInMemory) {
            _memoryTimer -= Time.deltaTime;
            if (_memoryTimer <= 0f) _playerInMemory = false;
        }

        switch (currentState) {
            case State.Idle:
                agent.speed = patrolSpeed;
                if (_playerInMemory) EnterChase();
                else Patrol();
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                if (!_playerInMemory) {
                    EnterSearch();
                    break;
                }

                // Unity сама найдет путь через OffMeshLink, если он есть!
                agent.SetDestination(player.position);
                agent.isStopped = false;

                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= attackRange) {
                    agent.isStopped = true;
                    currentState = State.Attack;
                }
                break;

            case State.Search:
                agent.speed = patrolSpeed * 1.2f;

                // Просто идём в последнюю известную точку
                agent.SetDestination(lastKnownPosition);
                agent.isStopped = false;
                CheckIfStuck();

                if (_playerInMemory) {
                    EnterChase();
                    break;
                }

                _searchTimer += Time.deltaTime;
                if (_searchTimer > searchGiveUpTime) {
                    currentState = State.Idle;
                    break;
                }

                if (!agent.pathPending && agent.remainingDistance < 1f && agent.pathStatus == NavMeshPathStatus.PathComplete) {
                    waitTimer += Time.deltaTime;
                    if (waitTimer > 2f)
                        currentState = State.Idle;
                }
                break;

            case State.Attack:
                attackTimer -= Time.deltaTime;
                LookAtPlayer();

                if (attackTimer <= 0f) {
                    Debug.Log("АТАКА!");
                    attackTimer = attackCooldown;
                }

                float d = Vector3.Distance(transform.position, player.position);
                if (d > attackRange + 0.5f) {
                    agent.isStopped = false;
                    currentState = State.Chase;
                } else if (!_playerInMemory) {
                    agent.isStopped = false;
                    currentState = State.Search;
                }
                break;
        }
    }

    // --- НОВЫЕ МЕТОДЫ ---

    /// <summary>
    /// Проверяет, есть ли полный путь до точки (через NavMesh или OffMeshLinks)
    /// </summary>
    bool HasCompletePathTo(Vector3 target) {
        NavMeshPath path = new NavMeshPath();
        bool hasPath = NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path);
        return hasPath && path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// Находит ближайшую точку на краю NavMesh, откуда можно перейти к цели
    /// </summary>
    Vector3 FindNearestOffMeshEntry(Vector3 target) {
        // Ищем ближайшую точку на NavMesh к цели
        if (NavMesh.SamplePosition(target, out NavMeshHit targetHit, 10f, NavMesh.AllAreas)) {
            // Цель на NavMesh, но на другом острове
            // Ищем точку на нашем острове, ближайшую к целевому острову

            // Вариант 1: найти ближайший OffMeshLink
            OffMeshLink nearestLink = FindNearestOffMeshLink(targetHit.position);
            if (nearestLink != null) {
                // Возвращаем точку Start или End, в зависимости от того, что ближе к нам
                float distToStart = Vector3.Distance(transform.position, nearestLink.startTransform.position);
                float distToEnd = Vector3.Distance(transform.position, nearestLink.endTransform.position);

                return distToStart < distToEnd ? nearestLink.startTransform.position : nearestLink.endTransform.position;
            }
        }

        // Вариант 2: просто идём к ближайшей точке на NavMesh к цели (на нашем острове)
        // Это приведёт нас к краю платформы
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(transform.position, target, NavMesh.AllAreas, path) && path.corners.Length > 0) {
            // Последняя достижимая точка на нашем острове
            return path.corners[path.corners.Length - 1];
        }

        return Vector3.zero;
    }

    OffMeshLink FindNearestOffMeshLink(Vector3 targetPos) {
        OffMeshLink[] allLinks = FindObjectsOfType<OffMeshLink>();
        OffMeshLink nearest = null;
        float minDist = float.MaxValue;

        foreach (var link in allLinks) {
            if (!link.activated) continue;

            float dist = Vector3.Distance(targetPos, link.transform.position);
            if (dist < minDist) {
                minDist = dist;
                nearest = link;
            }
        }

        return nearest;
    }

    void CheckIfStuck() {
        float moved = Vector3.Distance(transform.position, _lastPosition);
        if (moved < 0.1f) {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer > stuckThreshold) {
                TryJumpToTarget();
                _stuckTimer = 0f;
            }
        } else {
            _stuckTimer = 0f;
        }
        _lastPosition = transform.position;
    }

    void TryJumpToTarget() {
        // Убрал EnemyJump — теперь прыжок только через OffMeshLinkJump
        // var jumper = GetComponent<EnemyJump>();
        // if (jumper != null) jumper.ForceJumpTo(lastKnownPosition);

        // Вместо этого: если застряли, пробуем найти OffMeshLink
        Vector3 entry = FindNearestOffMeshEntry(lastKnownPosition);
        if (entry != Vector3.zero) {
            agent.SetDestination(entry);
        }
    }

    bool CanSeePlayer() {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > detectionRange) return false;

        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle * 0.5f) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.6f;
        Vector3 targetPos = player.position + Vector3.up * 1.0f;
        Vector3 rayDir = targetPos - eyePos;

        if (Physics.Raycast(eyePos, rayDir.normalized, out RaycastHit hit, detectionRange, visionBlockMask, QueryTriggerInteraction.Ignore)) {
            if (!hit.transform.CompareTag("Player")) return false;
        }

        return true;
    }

    void EnterChase() {
        currentState = State.Chase;
        agent.isStopped = false;
        _searchTimer = 0f;
        _stuckTimer = 0f;
        _goingToOffMesh = false;
    }

    void EnterSearch() {
        currentState = State.Search;
        waitTimer = 0f;
        _searchTimer = 0f;
        _stuckTimer = 0f;
        _lastPosition = transform.position;
        _goingToOffMesh = false;
    }

    void LookAtPlayer() {
        Vector3 lookPos = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(lookPos);
    }

    void Patrol() {
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

    void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Vector3 left = Quaternion.Euler(0, -viewAngle / 2f, 0) * transform.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle / 2f, 0) * transform.forward;
        Gizmos.DrawLine(transform.position, transform.position + left * detectionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * detectionRange);

        if (currentState == State.Search || currentState == State.Chase) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }

        // Рисуем путь агента
        if (agent.hasPath) {
            Gizmos.color = Color.magenta;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
                Gizmos.DrawLine(corners[i], corners[i + 1]);
        }
    }
}