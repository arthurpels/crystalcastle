using UnityEngine;

public abstract class HandItem : MonoBehaviour
{
    public ItemData data; // Для доступа к иконке/названию (опционально)

    public HandRigConfig rigConfig = new HandRigConfig
    {
        behavior = HandRigConfig.RigBehavior.AimAtCamera,
        AimWeight = 0.9f,
        blendInSpeed = 5f,
        blendOutSpeed = 3f,
        maxAngle = 60f
    };
    
    // Вызывается при экипировке
    public abstract void OnEquip();
    public abstract void OnUse();
    public abstract void OnUnequip();
     public virtual void OnTick(float dt) {} // Для батарейки, кулдаунов
}