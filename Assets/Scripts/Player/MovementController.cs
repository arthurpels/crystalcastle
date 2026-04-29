using UnityEngine;

/// <summary>
/// контроллер движения.
/// Отвечает только за: ввод → вектор движения → применение к CharacterController.
/// НЕ отвечает за: камеру, анимации, звук, здоровье, инвентарь.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(-10)] // Выполняется ДО других систем, чтобы они читали актуальное состояние
public class MovementController : MonoBehaviour {
    #region === Настройки ===

    [Header("Speed")]
    [Tooltip("Базовая скорость ходьбы")]
    [SerializeField] private float baseMoveSpeed = 2.0f;
    [Tooltip("Базовая скорость бега")]
    [SerializeField] private float baseSprintSpeed = 5.335f;
    [Tooltip("Скорость разгона/торможения (чем больше, тем резче)")]
    [SerializeField] private float speedChangeRate = 10.0f;

    [Header("Rotate")]
    [Tooltip("Время плавного доворота в сторону движения")]
    [Range(0.0f, 0.3f)]
    [SerializeField] private float rotationSmoothTime = 0.12f;

    [Header("Gravity and Jump")]
    [Tooltip("Сила гравитации (отрицательная)")]
    [SerializeField] private float gravity = -15.0f;
    [Tooltip("Высота прыжка")]
    [SerializeField] private float jumpHeight = 1.2f;
    [Tooltip("Задержка между прыжками")]
    [SerializeField] private float jumpTimeout = 0.50f;
    [Tooltip("Максимальная скорость падения")]
    [SerializeField] private float terminalVelocity = 53.0f;

    [Header("Ground")]
    [Tooltip("Смещение сферы проверки заземления (отрицательное = ниже позиции)")]
    [SerializeField] private float groundedOffset = -0.14f;
    [Tooltip("Радиус сферы проверки (должен совпадать с радиусом контроллера)")]
    [SerializeField] private float groundedRadius = 0.28f;
    [Tooltip("Слои, которые считаются землёй")]
    [SerializeField] private LayerMask groundLayers;


    #endregion

    #region === Публичный интерфейс===

    /// <summary>Множитель скорости от поверхностей/погоды/статусов</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>Множитель сцепления (влияет на ускорение/торможение)</summary>
    public float GripMultiplier { get; set; } = 1f;

    /// <summary>Можно ли принимать ввод (для пауз, катсцен)</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Касается ли земли</summary>
    public bool IsGrounded => grounded;

    #endregion

    #region === Внутреннее состояние ===

    // Ввод (заполняется извне через SetInput)
    private Vector2 moveInput;
    private bool sprintInput;
    private bool jumpInput;

    // Физика
    private CharacterController controller;
    private float verticalVelocity;
    private float currentSpeed;
    private float animationBlend; // для передачи в Animator извне
    private bool grounded;

    // Таймауты
    private float jumpTimeoutDelta;

    // Поворот
    private float targetRotation;
    private float rotationVelocity;

    // Кэш
    private Transform mainCameraTransform;

    // Константы
    private const float InputThreshold = 0.01f;
    private const float GroundedVelocityReset = -2f;

    private Vector3 _hitNormal = Vector3.up;
    private bool _isTouchingSurface = false;


    #endregion

    #region === Unity Events ===

    private void Awake() {
        controller = GetComponent<CharacterController>();
        mainCameraTransform = Camera.main?.transform;

        // Инициализация таймаутов
        jumpTimeoutDelta = jumpTimeout;
    }

    private void Update() {
        if (!InputEnabled) return;

        CheckGrounded();
        ApplyGravityAndJump();
        ApplyMovement();

    }

    #endregion

    #region === Публичные методы===

    /// <summary>
    /// Основной метод для передачи ввода.Будем вызывать из PlayerInputHandler.
    /// </summary>
    public void SetInput(Vector2 move, bool sprint, bool jump) {
        moveInput = move;
        sprintInput = sprint && InputEnabled;
        jumpInput = jump && InputEnabled;
    }

    /// <summary>
    /// Возвращает нормализованную скорость для анимаций
    /// </summary>
    public float GetAnimationBlend() => animationBlend;

    #endregion

    #region === Математика движения ===

    private void CheckGrounded() {
        // проверка: сфера со смещением, игнорируя триггеры
        Vector3 spherePosition = transform.position + Vector3.up * groundedOffset;
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void ApplyGravityAndJump() {
        if (grounded) {
            // Сброс таймаута падения

            // Сброс вертикальной скорости при приземлении
            if (verticalVelocity < 0f)
                verticalVelocity = GroundedVelocityReset;

            // Обработка прыжка
            if (jumpInput && jumpTimeoutDelta <= 0f) {
                // формула: v = √(2 * g * h)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpInput = false; // Сброс toggle-а
            }

            // Таймаут прыжка
            if (jumpTimeoutDelta > 0f)
                jumpTimeoutDelta -= Time.deltaTime;
        } else {
            // В воздухе: сброс таймаута прыжка, отсчёт до "падения"
            jumpTimeoutDelta = jumpTimeout;

            // Блокировка прыжка в воздухе
            jumpInput = false;
        }

        // Применение гравитации с ограничением максимальной скорости
        if (verticalVelocity > terminalVelocity)
            verticalVelocity = terminalVelocity;

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyMovement() {
        // 1. Если нет ввода — плавно тормозим до 0
        float targetSpeed = (moveInput.sqrMagnitude < InputThreshold)
            ? 0f
            : (sprintInput ? baseSprintSpeed : baseMoveSpeed);

        // 2. Применяем модификатор к скорости
        targetSpeed *= SpeedMultiplier;

        // 3. нелинейная акселерация
        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;
        float speedOffset = 0.1f;
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude); // Для аналогового джойстика

        // Применяем модификатор сцепления 
        float effectiveChangeRate = speedChangeRate * GripMultiplier;

        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset) {
            // Lerp даёт изменение скорости, что является ускорением
            currentSpeed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * effectiveChangeRate);
        } else {
            currentSpeed = targetSpeed;
        }

        // Затухание анимаций
        animationBlend = Mathf.Lerp(animationBlend, targetSpeed, Time.deltaTime * effectiveChangeRate);
        if (animationBlend < 0.01f) animationBlend = 0f;

        // 4. Поворот 
        if (moveInput.sqrMagnitude >= InputThreshold && mainCameraTransform != null) {
            // Направление относительно камеры (без наклона по Y)
            Vector3 camForward = mainCameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = mainCameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 MoveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            // Целевой угол поворота
            targetRotation = Mathf.Atan2(MoveDir.x, MoveDir.z) * Mathf.Rad2Deg;

            // Плавный поворот через SmoothDamp
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation,
                ref rotationVelocity, rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }



        // 5. Финальный вектор движения
        Vector3 moveDirection = Quaternion.Euler(0f, targetRotation, 0f) * Vector3.forward;
        Vector3 finalVelocity = moveDirection.normalized * currentSpeed + Vector3.up * verticalVelocity;

        CalculateSlopeSliding(ref finalVelocity);

        // 6. Применение к контроллеру
        controller.Move(finalVelocity * Time.deltaTime);


    }

    private void OnControllerColliderHit(ControllerColliderHit hit) {
        // Берем нормаль, только если ударился нижней частью капсулы
        // (чтобы не скользить, задев стену головой)
        if (hit.normal.y > 0f) {
            _hitNormal = hit.normal;
            _isTouchingSurface = true;
        }
    }

    private void CalculateSlopeSliding(ref Vector3 moveVector) {
        if (!grounded && _isTouchingSurface) {            
            float slideFactor = 1f - _hitNormal.y;

            // Добавляем боковую скорость вдоль склона
            moveVector.x += slideFactor * _hitNormal.x;
            moveVector.z += slideFactor * _hitNormal.z;

            if (verticalVelocity < 0) {
                verticalVelocity = GroundedVelocityReset; //перс будет плавно соскадьзывать и не притянется к земле в конце скольжения а плавно начнет падать
            }
            
            _isTouchingSurface = false;
        }
    }

    #endregion

    #region === Отладка ===

    private void OnDrawGizmosSelected() {
        // Визуализация сферы заземления в редакторе
        Color gizmoColor = grounded ? new Color(0f, 1f, 0f, 0.35f) : new Color(1f, 0f, 0f, 0.35f);
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position + Vector3.up * groundedOffset, groundedRadius);


        // Gizmos.DrawSphere(transform.position + Vector3.up * slopeOffset, slopeRadius);
    }

    #endregion
}