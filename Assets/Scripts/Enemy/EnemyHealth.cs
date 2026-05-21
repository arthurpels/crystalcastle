using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IHealth
{
    [SerializeField] private float maxHP = 50f;
    [SerializeField] private float disappearDelay = 3f; // секунд до исчезновения
    
    public float CurrentHP { get; private set; }
    public float MaxHP => maxHP;
    public bool IsAlive => CurrentHP > 0;
    
    public event System.Action<float> OnHPChanged;
    public event System.Action OnDeath;
    public event System.Action<float> OnDamaged; 

    void Awake() => CurrentHP = maxHP;

    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP);
        OnDamaged?.Invoke(amount); // НОВОЕ
        
        GetComponent<SimpleEnemyAI>()?.OnDamaged(amount);
        
        if (!IsAlive)
        {
            OnDeath?.Invoke();
            StartCoroutine(DieSequence());
        }
    }

    private IEnumerator DieSequence()
    {
        // Отключаем AI
        var ai = GetComponent<SimpleEnemyAI>();
        if (ai != null) ai.enabled = false;
        
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        
        // TODO: ragdoll, частицы смерти, звук
        
        yield return new WaitForSeconds(disappearDelay);
        
        // Исчезаем
        Destroy(gameObject);
    }
}