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
    private static readonly int HashGrounded = Animator.StringToHash("Grounded");
    private static readonly int HashMoveSpeed = Animator.StringToHash("MoveSpeed");
    private static readonly int HashSprint = Animator.StringToHash("Sprint");
    private static readonly int HashVerticalVel = Animator.StringToHash("VerticalVelocity"); // Опционально

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        if (movement == null) movement = GetComponent<MovementController>();
    }

    private void LateUpdate()
    {
        if (movement == null || _animator == null) return;

        // 1. Земля
        _animator.SetBool(HashGrounded, movement.IsGrounded);

        // 2. Скорость (Blend Tree: Idle → Walk → Run → Sprint)
        _animator.SetFloat(HashMoveSpeed, movement.GetAnimationBlend());

        // 3. Факт бега (если скорость превысила порог walk)
        _animator.SetBool(HashSprint, movement.IsSprinting);

        // 4. Вертикальная скорость (для анимаций взлёта/падения/приземления)
        _animator.SetFloat(HashVerticalVel, movement.VerticalVelocity);
    }
}