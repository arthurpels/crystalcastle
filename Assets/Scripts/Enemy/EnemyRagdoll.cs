using UnityEngine;

public class EnemyRagdoll : MonoBehaviour
{
    private Rigidbody[] _bones;
    private Collider[]  _boneColliders;
    private Collider    _rootCollider;
    private Animator    _animator;

    private void Awake()
    {
        _animator     = GetComponent<Animator>();
        _rootCollider = GetComponent<Collider>();

        // Кости — только дочерние Rigidbody (не корень)
        _bones         = GetComponentsInChildren<Rigidbody>();
        _boneColliders = GetComponentsInChildren<Collider>();

        SetBonesKinematic(true);
        SetBoneCollidersEnabled(false);
    }

    public void Activate()
    {
        if (_rootCollider != null)
            _rootCollider.enabled = false;

        if (_animator != null)
            _animator.enabled = false;

        SetBoneCollidersEnabled(true);
        SetBonesKinematic(false);
    }

    private void SetBonesKinematic(bool kinematic)
    {
        foreach (Rigidbody rb in _bones)
            rb.isKinematic = kinematic;
    }

    private void SetBoneCollidersEnabled(bool enabled)
    {
        foreach (Collider col in _boneColliders)
            col.enabled = enabled;
    }
}
