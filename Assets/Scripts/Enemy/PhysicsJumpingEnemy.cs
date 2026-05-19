using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class PhysicsJumpingEnemy : MonoBehaviour
{
    [Header("Ссылки")]
    public Transform player;            // Ссылка на трансформ игрока

    [Header("Физика прыжка")]
    public float jumpUpForce = 8f;      // Сила прыжка вверх
    public float jumpForwardForce = 4f; // Сила толчка вперед (чтобы залететь НА препятствие)
    public float jumpCooldown = 1.5f;   // Перезарядка прыжка

    [Header("Настройки лучей")]
    public LayerMask obstacleLayer;     // Слой препятствий/стен/ящиков
    public float checkDistance = 1.5f;  // Расстояние до стены, чтобы понять, что надо прыгать

    private NavMeshAgent agent;
    private Rigidbody rb;
    private bool isJumping = false;
    private float nextJumpTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // По умолчанию физика отключена, движением управляет NavMeshAgent
        rb.isKinematic = true; 
        agent.updatePosition = true;
        agent.updateRotation = true;
    }

    void Update()
    {
        if (player == null) return;

        // Если мы в полете, логика NavMesh и поиска путей полностью блокируется
        if (isJumping) return;

        HandleMovementAndPath();
        CheckJumpOrDrop();
    }

    void HandleMovementAndPath()
    {
        // Проверяем, может ли NavMesh дойти прямо до игрока
        NavMeshPath path = new NavMeshPath();
        agent.CalculatePath(player.position, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            // Путь чист — бежим напрямую к игроку
            agent.SetDestination(player.position);
        }
        else
        {
            // Если игрок запрыгнул на ящик (путь заблокирован),
            // принудительно заставляем агента бежать к ближайшей точке NavMesh под этим ящиком
            if (NavMesh.SamplePosition(player.position, out NavMeshHit navHit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
            }
        }
    }

    void CheckJumpOrDrop()
    {
        if (Time.time < nextJumpTime) return;

        float heightDifference = player.position.y - transform.position.y;

        // СИТУАЦИЯ 1: Игрок НАВЕРХУ (Нужно запрыгнуть)
        if (heightDifference > 0.8f && Vector3.Distance(transform.position, player.position) < 4f)
        {
            // Пускаем луч перед собой, чтобы проверить, уперлись ли мы в препятствие
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(rayStart, transform.forward, checkDistance, obstacleLayer))
            {
                PhysicsJump(true); // Вызываем прыжок вверх
            }
        }
        // СИТУАЦИЯ 2: Игрок ВНИЗУ, а враг на уступе (Нужно спрыгнуть)
        else if (heightDifference < -1.2f && Vector3.Distance(transform.position, player.position) < 5f)
        {
            // Если агент дошел до края сетки (дальше идти не может) и игрок внизу
            if (agent.remainingDistance <= agent.stoppingDistance + 0.3f)
            {
                PhysicsJump(false); // Вызываем силовой прыжок/спрыгивание вперед
            }
        }
    }

    void PhysicsJump(bool jumpingUp)
    {
        isJumping = true;
        nextJumpTime = Time.time + jumpCooldown;

        // Ключевой шаг: полностью отключаем NavMeshAgent, иначе он прижмет физику обратно к полу
        agent.enabled = false;

        // Включаем симуляцию физики в Rigidbody
        rb.isKinematic = false;

        Vector3 jumpVelocity;

        if (jumpingUp)
        {
            // Импульс высоко вверх + немного вперед
            jumpVelocity = (Vector3.up * jumpUpForce) + (transform.forward * jumpForwardForce);
        }
        else
        {
            // Если спрыгиваем вниз — даем сильный толчок только вперед (вверх почти не прыгаем)
            jumpVelocity = (Vector3.up * (jumpUpForce * 0.3f)) + (transform.forward * (jumpForwardForce * 1.5f));
        }

        // Толкаем тело через честную физику Unity
        rb.AddForce(jumpVelocity, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Если враг был в состоянии прыжка и приземлился на что-то
        if (isJumping)
        {
            // Игнорируем столкновения с игроком в полете, чтобы не гасить импульс
            if (collision.gameObject.CompareTag("Player")) return;

            // Возвращаем врага в режим навигации
            isJumping = false;
            rb.isKinematic = true; // Отключаем Rigidbody, чтобы он не скользил по инерции
            agent.enabled = true;  // Возвращаем контроль NavMeshAgent
        }
    }
}
