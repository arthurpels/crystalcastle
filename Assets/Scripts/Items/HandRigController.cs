using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Применяет настройки ригга от экипированного предмета к руке.
/// Работает с новой архитектурой: читает rigConfig напрямую из HandItem.
/// </summary>
public class HandRigController : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [SerializeField] private ItemSlot itemSlot;          // Слот правой руки
    [SerializeField] private MultiAimConstraint aimConstraint; // Твой констрейнт из рига
    
    private HandRigConfig _currentConfig;
    private float _currentWeight;

    private void LateUpdate()
    {
        // 1. Определяем целевой вес
        float targetWeight = 0f;
        
        // Проверяем, есть ли предмет в слоте и есть ли у него конфиг
        if (itemSlot?.CurrentItem != null && itemSlot.CurrentItem is HandItem handItem)
        {
            var newConfig = handItem.rigConfig;
            
            // Если конфиг сменился (новый предмет) — сбрасываем вес для плавного перехода
            if (newConfig != _currentConfig)
            {
                _currentConfig = newConfig;
                _currentWeight = 0f;
            }

            // Проверяем, активен ли предмет (фонарик горит)
            // Для фонарика: проверяем IsOn, для монтировки: всегда активно, если в руке
            bool isActive = CheckItemActive(handItem);
            
            if (isActive && _currentConfig.behavior == HandRigConfig.RigBehavior.AimAtCamera)
                targetWeight = _currentConfig.AimWeight;
        }

        // 2. Плавная интерполяция веса (Blend In / Blend Out)
        if (_currentConfig != null)
        {
            float speed = targetWeight > _currentWeight ? _currentConfig.blendInSpeed : _currentConfig.blendOutSpeed;
            _currentWeight = Mathf.Lerp(_currentWeight, targetWeight, Time.deltaTime * speed);
        }
        else
        {
            // Если конфига нет, просто гасим вес
            _currentWeight = Mathf.Lerp(_currentWeight, 0f, Time.deltaTime * 3f);
        }

        // 3. Применяем к констрейнту
        if (aimConstraint != null)
            aimConstraint.weight = _currentWeight;
    }

    // Проверка: должен ли предмет управлять рукой прямо сейчас?
    private bool CheckItemActive(HandItem item)
    {
        // Фонарик: только если включен
        if (item is FlashlightHandItem flash) return flash.IsOn;
        
        // Монтировка/другое оружие: всегда активно, пока в руке
        // Если нужно усложнить (например, только при зажатой атаке) — добавь логику сюда
        return true;
    }
}