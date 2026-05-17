using UnityEngine;

public class FrozenEnemy : MonoBehaviour {
    [SerializeField] private MonoBehaviour aiComponent;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider damageCollider;
    [SerializeField] private bool isFrozen = true;

    void Start() {
        if (isFrozen) SetFrozen(true);
    }

    public void Thaw(float delay = 0f) {
        if (!isFrozen) return;
        if (delay > 0f) Invoke(nameof(DoThaw), delay);
        else DoThaw();
    }

    void DoThaw() {
        isFrozen = false;
        SetFrozen(false);
        animator?.SetTrigger("WakeUp");
    }

    void SetFrozen(bool frozen) {
        if (aiComponent != null) aiComponent.enabled = !frozen;
        if (damageCollider != null) damageCollider.enabled = !frozen;
        animator?.SetBool("Frozen", frozen);
    }
}