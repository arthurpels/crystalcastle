using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyHealth : MonoBehaviour, IHealth, ISaveable
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
        
        var ai = GetComponent<SimpleEnemyAI>();
        if (ai != null) ai.OnDamaged(amount);
        
        if (!IsAlive)
        {
            OnDeath?.Invoke();
            StartCoroutine(DieSequence());
        }
    }

    private IEnumerator DieSequence()
    {
        // Уведомляем SaveManager ДО Destroy
        if (SaveManager.Instance != null)
            SaveManager.Instance.RegisterDestroyed(this);

        var ai = GetComponent<SimpleEnemyAI>();
        if (ai != null) ai.enabled = false;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var ragdoll = GetComponent<EnemyRagdoll>();
        if (ragdoll != null) ragdoll.Activate();

        var audio = GetComponent<EnemyAudioController>();
        if (audio != null) audio.OnDeath();

        yield return new WaitForSeconds(disappearDelay);
        Destroy(gameObject);
    }

    // ── ISaveable ──────────────────────────────────────────────────────────

    [System.Serializable]
    private struct EnemySaveData
    {
        public float px, py, pz, ry;   // transform
        public float hp;
        public bool  isFrozen;
        public int   aiState;           // SimpleEnemyAI.State (int)
        public float lkpX, lkpY, lkpZ; // lastKnownPosition
    }

    public string CaptureState()
    {
        var frozen = GetComponent<FrozenEnemy>();
        var ai     = GetComponent<SimpleEnemyAI>();
        var lkp    = ai != null ? ai.LastKnownPosition : Vector3.zero;

        return JsonUtility.ToJson(new EnemySaveData
        {
            px = transform.position.x,
            py = transform.position.y,
            pz = transform.position.z,
            ry = transform.eulerAngles.y,
            hp = CurrentHP,
            isFrozen = frozen != null && frozen.IsFrozen,
            aiState  = ai != null ? (int)ai.CurrentState : 0,
            lkpX = lkp.x, lkpY = lkp.y, lkpZ = lkp.z
        });
    }

    public void RestoreState(string json)
    {
        var d = JsonUtility.FromJson<EnemySaveData>(json);

        // Позиция через NavMeshAgent.Warp (не ломает агент)
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh)
            agent.Warp(new Vector3(d.px, d.py, d.pz));
        else
            transform.position = new Vector3(d.px, d.py, d.pz);

        transform.rotation = Quaternion.Euler(0f, d.ry, 0f);

        // HP
        CurrentHP = d.hp;
        OnHPChanged?.Invoke(CurrentHP);

        // Заморозка
        var frozen = GetComponent<FrozenEnemy>();
        if (frozen != null) frozen.RestoreForLoad(d.isFrozen);

        // AI-состояние
        var ai = GetComponent<SimpleEnemyAI>();
        if (ai != null) ai.RestoreForLoad(
            (SimpleEnemyAI.State)d.aiState,
            new Vector3(d.lkpX, d.lkpY, d.lkpZ));
    }
}