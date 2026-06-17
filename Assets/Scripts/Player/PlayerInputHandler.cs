using UnityEngine;
using UnityEngine.InputSystem;
using System;

/// <summary>
/// Чистый обработчик ввода. Читает действия, буферизует прыжок, отдает готовые данные в MovementController.
/// </summary>
[RequireComponent(typeof(MovementController))]
[RequireComponent(typeof(PlayerInventory))]
[RequireComponent(typeof(InteractionManager))]
public class PlayerInputHandler : MonoBehaviour {



    [Header("Config")]
    [Tooltip("Время хранения нажатия прыжка (сек). Позволяет 'спамить' до приземления")]
    [Range(0f, 0.5f)][SerializeField] private float jumpBufferTime = 0.1f;


    [SerializeField] private InventoryUI inventoryUI;

    [SerializeField] private PauseMenu pauseMenu;

    private PlayerInputAction playerInputAction;
    // Публичный интерфейс
    public bool InputEnabled { get; set; } = true;
    public bool InputEnabledSoft { get; set; } = true;
    public Vector2 MoveInput { get; private set; }
    public event Action JumpPressed;

    // Внутреннее состояние
    private MovementController _movementController;
    private PlayerInventory _playerInventory;
    private InteractionManager _interactionManager;


    private float _jumpBufferTimer;


    public void SetInputEnabled(bool enabled) {
        InputEnabled = enabled;
    }

    public void SetInputEnabledSoft(bool enabled) {
        InputEnabledSoft = enabled;
    }
    
    private void Awake() {

        playerInputAction = new PlayerInputAction();
        _movementController = GetComponent<MovementController>();
        _playerInventory = GetComponent<PlayerInventory>();
        _interactionManager = GetComponent<InteractionManager>();
        if (playerInputAction == null) {
            Debug.LogError($"[{name}] Missing InputActions Asset.", this);
            enabled = false; return;
        }

        // Подписка на событие прыжка
        playerInputAction.Player.Jump.performed += _ => TriggerJumpBuffer();
    }

    private void OnEnable() => playerInputAction.Enable();
    private void OnDisable() => playerInputAction.Disable();

    private void Update() {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) {
            // ESC дёргает паузу только если мы уже на паузе (чтобы её закрыть)
            // или если никакой другой UI не захватил ввод (квест/документ ставят InputEnabled=false).
            if (pauseMenu != null && (pauseMenu.IsPaused || InputEnabled))
                pauseMenu.Toggle();
        }

        if (!InputEnabled) { StopMovement(); return; }

        if (Keyboard.current.tabKey.wasPressedThisFrame) { // или iKey
            inventoryUI.Toggle();
        }

        if (!InputEnabledSoft) { StopMovement(); return; }

        if (_jumpBufferTimer > 0f) _jumpBufferTimer -= Time.deltaTime;

        MoveInput = playerInputAction.Player.Move.ReadValue<Vector2>();
        bool sprint = playerInputAction.Player.Sprint.IsPressed();
        bool jump = _jumpBufferTimer > 0f;

        _movementController.SetInput(MoveInput, sprint, jump);

        if (Mouse.current.leftButton.wasPressedThisFrame) {
            _playerInventory.UseRightHandItem();
        }

        if (Keyboard.current.fKey.wasPressedThisFrame) {
            _playerInventory.UseLeftHandItem();
        }

        if (Keyboard.current.qKey.wasPressedThisFrame) {
            _playerInventory.DropFromSlot(_playerInventory.rightHandSlot);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame) {
            _interactionManager.TryInteract();
        }

        

        // if (Keyboard.current.gKey.wasPressedThisFrame)
        //     _itemManager.OnRightHandAction();
    }

    private void TriggerJumpBuffer() {
        if (!InputEnabled) return;
        _jumpBufferTimer = jumpBufferTime;
        JumpPressed?.Invoke();
    }

    /// <summary>Вызвать прыжок из кода (способности, триггеры, ИИ)</summary>
    public void RequestJump() => TriggerJumpBuffer();
    /// <summary>Сбросить состояние ввода (для пауз, катсцен)</summary>
    public void ResetInput() => _jumpBufferTimer = 0f;

    /// <summary>
    /// Обнулить ввод движения, чтобы кнопки не "залипали" при отключении
    /// управления (вход в инвентарь, генератор и т.п.). Вызывается каждый кадр,
    /// пока управление отключено, — поэтому персонаж тормозит, а не катится
    /// дальше на последнем прочитанном вводе.
    /// </summary>
    private void StopMovement() {
        MoveInput = Vector2.zero;
        _jumpBufferTimer = 0f;
        _movementController.SetInput(Vector2.zero, false, false);
    }
}