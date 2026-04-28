using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Чистый обработчик ввода. Читает действия, буферизует прыжок, отдает готовые данные в MovementController.
/// </summary>
[RequireComponent(typeof(MovementController))]
public class PlayerInputHandler : MonoBehaviour
{
    
    
    
    [Header("Config")]
    [Tooltip("Время хранения нажатия прыжка (сек). Позволяет 'спамить' до приземления")]
    [Range(0f, 0.5f)] [SerializeField] private float jumpBufferTime = 0.1f;

    
    private PlayerInputAction inputActionsAsset;
    // Публичный интерфейс
    public bool InputEnabled { get; set; } = true;
    public Vector2 MoveInput { get; private set; }
    public event Action JumpPressed;

    // Внутреннее состояние
    private MovementController _movementController;
    private float _jumpBufferTimer;

    private void Awake() {

        inputActionsAsset = new PlayerInputAction();
        _movementController = GetComponent<MovementController>();
        if (inputActionsAsset == null)
        {
            Debug.LogError($"[{name}] Missing InputActions Asset.", this);
            enabled = false; return;
        }

        // Подписка на событие прыжка
        inputActionsAsset.Player.Jump.performed += _ => TriggerJumpBuffer();
    }

    private void OnEnable() => inputActionsAsset.Enable();
    private void OnDisable() => inputActionsAsset.Disable();

    private void Update()
    {
        if (!InputEnabled) return;

        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;

        MoveInput = inputActionsAsset.Player.Move.ReadValue<Vector2>();
        bool sprint = inputActionsAsset.Player.Sprint.IsPressed();
        bool jump = _jumpBufferTimer > 0f;

        _movementController.SetInput(MoveInput, sprint, jump);
    }

    private void TriggerJumpBuffer()
    {
        if (!InputEnabled) return;
        _jumpBufferTimer = jumpBufferTime;
        JumpPressed?.Invoke();
    }

    /// <summary>Вызвать прыжок из кода (способности, триггеры, ИИ)</summary>
    public void RequestJump() => TriggerJumpBuffer();
    /// <summary>Сбросить состояние ввода (для пауз, катсцен)</summary>
    public void ResetInput() => _jumpBufferTimer = 0f;
}