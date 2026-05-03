using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI-контроллер, подписывается на события PlayerAttributes и обновляет полосы.
/// Не опрашивает состояние в Update. Работает только на событиях.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Ссылки на логику")]
    [SerializeField] private PlayerAttributes playerAttributes;

    [Header("Элементы HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private GameObject hpContainer;

    [Header("Элементы Stamina")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private GameObject staminaContainer;

    [Header("Настройки")]
    [Tooltip("Скрывать полоску стамины, когда она на 100%")]
    [SerializeField] private bool hideStaminaWhenFull = true;

    private void Awake()
    {
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
        
        if (playerAttributes == null)
        {
            Debug.LogError("[PlayerHUD] PlayerAttributes not found!", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        playerAttributes.OnHealthChanged += UpdateHealthBar;
        playerAttributes.OnStaminaChanged += UpdateStaminaBar;
        playerAttributes.OnDeath += HandleDeath;
        
        // Инициализация текущего состояния
        UpdateHealthBar(playerAttributes.CurrentHealth, playerAttributes.MaxHealth);
        UpdateStaminaBar(playerAttributes.CurrentStamina, playerAttributes.MaxStamina);
    }

    private void OnDisable()
    {
        // Обязательно отписываемся, чтобы не было утечек и ошибок
        if (playerAttributes != null)
        {
            playerAttributes.OnHealthChanged -= UpdateHealthBar;
            playerAttributes.OnStaminaChanged -= UpdateStaminaBar;
            playerAttributes.OnDeath -= HandleDeath;
        }
    }

    private void UpdateHealthBar(float current, float max)
    {
        if (hpFill != null)
            hpFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaFill == null) return;
        
        staminaFill.fillAmount = max > 0 ? current / max : 0f;
        
        if (hideStaminaWhenFull && staminaContainer != null)
        {
            staminaContainer.SetActive(current < max - 1f); // Небольшой допуск, чтобы не мигало
        }
    }

    private void HandleDeath()
    {
        if (hpContainer != null) hpContainer.SetActive(false);
        if (staminaContainer != null) staminaContainer.SetActive(false);
        // Здесь можно показать панель смерти, затемнить экран и т.д.
    }
}