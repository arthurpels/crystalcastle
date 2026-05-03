using UnityEngine;

/// <summary>
/// Синхронизирует Animator с физическим состоянием персонажа.
/// Источник данных ТОЛЬКО MovementController. Input не используется.
/// </</summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField] private MovementController movement;
    
    private Animator _animator;
    private static readonly int animIDSpeed = Animator.StringToHash("Speed");
    private static readonly int animIDGrounded = Animator.StringToHash("Grounded");
    private static readonly int animIDJump = Animator.StringToHash("Jump");
    private static readonly int animIDFreeFall = Animator.StringToHash("FreeFall");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<MovementController>();
    }

    private void LateUpdate()
    {
        if (movement == null || _animator == null) return;

        _animator.SetFloat(animIDSpeed, movement.GetAnimationBlend());

        _animator.SetBool(animIDGrounded, movement.IsGrounded);
        if (movement.IsGrounded) {            
            _animator.SetBool(animIDJump, false);
            _animator.SetBool(animIDFreeFall, false);
        } else {
            _animator.SetBool(animIDFreeFall, movement.IsFalling);
        }

        if (movement.Jumped) {
            _animator.SetBool(animIDJump, true);
        }

        
    }
}