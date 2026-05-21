public interface IHealth
{
    float CurrentHP { get; }
    float MaxHP { get; }
    bool IsAlive { get; }
    
    void TakeDamage(float amount);
    
    event System.Action<float> OnHPChanged;  // текущее HP
    event System.Action<float> OnDamaged;    // сколько урона получил (НОВОЕ)
    event System.Action OnDeath;
}