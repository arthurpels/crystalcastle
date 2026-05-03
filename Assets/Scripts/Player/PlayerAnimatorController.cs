using UnityEngine;

/// <summary>
/// Синхронизирует Animator с физическим состоянием персонажа.
/// Источник данных ТОЛЬКО MovementController. Input не используется.
/// </</summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private MovementController movementController;
    
    private Animator _animator;
    private static readonly int animIDSpeed = Animator.StringToHash("Speed");
    private static readonly int animIDGrounded = Animator.StringToHash("Grounded");
    private static readonly int animIDJump = Animator.StringToHash("Jump");
    private static readonly int animIDFreeFall = Animator.StringToHash("FreeFall");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (movementController == null) movementController = GetComponent<MovementController>();
    }

    private void LateUpdate()
    {
        if (movementController == null || _animator == null) return;

        _animator.SetFloat(animIDSpeed, movementController.GetAnimationBlend());

        _animator.SetBool(animIDGrounded, movementController.IsGrounded);
        if (movementController.IsGrounded) {            
            _animator.SetBool(animIDJump, false);
            _animator.SetBool(animIDFreeFall, false);
        } else {
            _animator.SetBool(animIDFreeFall, movementController.IsFalling);
        }

        if (movementController.Jumped) {
            _animator.SetBool(animIDJump, true);
        }

        
    }
}