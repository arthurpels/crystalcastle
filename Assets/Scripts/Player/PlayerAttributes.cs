using UnityEngine;
using System;

/// <summary>
/// Управляет здоровьем, стаминой и базовыми состояниями игрока.
/// Событийно-ориентированная архитектура: UI и системы подписываются, а не опрашивают.
/// </summary>
public class PlayerAttributes : MonoBehaviour {
    #region === Настройки ===
    [Header("Links")]
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private MovementController movementController;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float healthRegenRate = 5f;
    [SerializeField] private float healthRegenDelay = 2f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 20f;
    [SerializeField] private float staminaRegenDelay = 1f;
    [Tooltip("Сколько стамины тратится в секунду при беге")]
    [SerializeField] private float sprintCostPerSecond = 15f;
    [SerializeField] private float jumpCostOnce = 50f;


    #endregion

    #region === Публичный интерфейс ===
    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
    public float CurrentHealth { get; private set; }
    public float CurrentStamina { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    public bool CanSprint => CurrentStamina > 1f; // Порог, чтобы не дёргать sprint на 0.01

    // События для UI, звука, анимаций, систем боя
    public event Action<float, float> OnHealthChanged;   // (current, max)
    public event Action<float, float> OnStaminaChanged;
    public event Action OnDeath;
    public event Action OnStaminaDepleted;
    public event Action OnStaminaRecovered;
    #endregion

    #region === Внутреннее состояние ===
    private float _healthRegenTimer;
    private float _staminaRegenTimer;
    private bool _isStaminaDepleted;
    #endregion

    private void Awake() {
        CurrentHealth = maxHealth;
        CurrentStamina = maxStamina;

        if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
        if (movementController == null) movementController = GetComponent<MovementController>();

        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
    }

    private void Update() {
        if (IsDead) return;

        UpdateStamina();
        UpdateRegeneration();
    }

    private void UpdateStamina() {

        if (movementController == null) return;
        
        if (movementController.Jumped) {
            TryConsumeStamina(jumpCostOnce);
        }
        
        if (movementController.IsSprinting) {
            TryConsumeStamina(sprintCostPerSecond * Time.deltaTime);
            _staminaRegenTimer = 0f;
        }
        _staminaRegenTimer += Time.deltaTime;

    }

    /// <summary>
    /// Пытается потратить стамину. Возвращает true, если хватает.
    /// Используется MovementController для проверки возможности бега.
    /// </summary>
    public bool TryConsumeStamina(float amount) {
        if (CurrentStamina >= amount) {
            ChangeStamina(-amount);
            return true;
        } else {
            ChangeStamina(-CurrentStamina);
            return false;
        }
    }

    private void ChangeStamina(float delta) {
        float oldStamina = CurrentStamina;
        CurrentStamina = Mathf.Clamp(CurrentStamina + delta, 0f, maxStamina);
        OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);

        // Отслеживаем переходы через порог
        if (oldStamina > 0f && CurrentStamina <= 0f && !_isStaminaDepleted) {
            _isStaminaDepleted = true;
            OnStaminaDepleted?.Invoke();
        } else if (CurrentStamina > 0f && _isStaminaDepleted) {
            _isStaminaDepleted = false;
            OnStaminaRecovered?.Invoke();
        }
    }

    public void TakeDamage(float amount) {
        if (IsDead) return;
        ChangeHealth(-amount);
        _healthRegenTimer = 0f;
    }

    public void Heal(float amount) {
        if (IsDead) return;
        ChangeHealth(amount);
    }

    private void ChangeHealth(float delta) {
        CurrentHealth = Mathf.Clamp(CurrentHealth + delta, 0f, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f && !IsDead)
            Die();
    }

    private void Die() {
        OnDeath?.Invoke();
        if (inputHandler != null) inputHandler.InputEnabled = false;
        // Здесь можно проиграть анимацию смерти, отключить коллайдеры, показать UI смерти и т.д.
    }

    private void UpdateRegeneration() {
        // Stamina regen
        if (_staminaRegenTimer >= staminaRegenDelay && CurrentStamina < maxStamina) {
            ChangeStamina(staminaRegenRate * Time.deltaTime);
        }

        // Health regen
        if (_healthRegenTimer >= healthRegenDelay && CurrentHealth < maxHealth) {
            ChangeHealth(healthRegenRate * Time.deltaTime);
        }
    }

    #region === Утилиты для UI и внешних систем ===
    public float GetHealthPercent() => maxHealth > 0 ? CurrentHealth / maxHealth : 0f;
    public float GetStaminaPercent() => maxStamina > 0 ? CurrentStamina / maxStamina : 0f;
    #endregion
}