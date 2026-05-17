using UnityEngine;
using UnityEngine.AI;

public class FrozenEnemy : MonoBehaviour {
    [SerializeField] private MonoBehaviour aiComponent;      // SimpleEnemyAI
    [SerializeField] private Animator animator;
    [SerializeField] private Collider damageCollider;
    [SerializeField] private bool startFrozen = true;

    [Header("Visual")]
    [SerializeField] private Renderer meshRenderer;
    [SerializeField] private Material frozenMaterial;
    [SerializeField] private Material normalMaterial;

    private NavMeshAgent _agent;

    public bool IsFrozen { get; private set; }

    void Awake() {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start() {
        if (startFrozen) Freeze();
    }

    public void Thaw(float delay = 0f) {
        Debug.Log($"[FrozenEnemy {name}] Thaw вызван, delay={delay}, isFrozen={IsFrozen}");
        if (!IsFrozen) return;
        if (delay > 0f) Invoke(nameof(DoThaw), delay);
        else DoThaw();
    }

    void DoThaw() {
        Debug.Log($"[FrozenEnemy {name}] DoThaw! Включаем AI...");
        
        IsFrozen = false;

        if (aiComponent != null)
            aiComponent.enabled = true;

        if (_agent != null) {
            _agent.isStopped = false;
            _agent.updateRotation = true;
        }

        if (damageCollider != null)
            damageCollider.enabled = true;

        animator?.SetTrigger("WakeUp");

        if (meshRenderer != null && normalMaterial != null)
            meshRenderer.material = normalMaterial;

        Debug.Log($"[{name}] ВРАГ ОЖИЛ!", this);
    }

    void Freeze() {
        IsFrozen = true;

        if (aiComponent != null)
            aiComponent.enabled = false;

        if (_agent != null) {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.updateRotation = false;
        }

        // if (damageCollider != null)
        //     damageCollider.enabled = false;

        animator?.SetBool("Frozen", true);

        if (meshRenderer != null && frozenMaterial != null)
            meshRenderer.material = frozenMaterial;
    }
}