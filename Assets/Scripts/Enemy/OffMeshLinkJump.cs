using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ручной траверс off-mesh связей: проигрывает дугу прыжка, когда агент
/// заходит на NavMeshLink (или legacy OffMeshLink).
///
/// «Off-mesh link» — общий термин Unity для обоих типов связей. Класс
/// работает с любым из них, потому что читает данные из
/// agent.currentOffMeshLinkData, а не из конкретного компонента связи.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class OffMeshLinkJump : MonoBehaviour {
    [Header("Прыжок")]
    [SerializeField] private float jumpSpeed = 4f;          // скорость вдоль связи, м/с
    [SerializeField] private float jumpArc = 1.2f;          // высота дуги над прямой
    [SerializeField] private float minJumpDuration = 0.25f; // минимальная длительность

    [Header("Анимация (опционально)")]
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";

    private NavMeshAgent agent;
    private Animator animator;
    private bool _traversing;

    /// <summary>Идёт ли сейчас прыжок через связь.</summary>
    public bool IsTraversing => _traversing;

    void Awake() {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        // Траверс ведём вручную — выставляем ОДИН раз здесь.
        // Если оставить значение по умолчанию (true), агент проскочит
        // связь сам, до того как корутина успеет её перехватить.
        agent.autoTraverseOffMeshLink = false;
    }

    void Update() {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        if (agent.isOnOffMeshLink && !_traversing)
            StartCoroutine(TraverseLink());
    }

    private IEnumerator TraverseLink() {
        _traversing = true;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 start = transform.position;                   // от текущей позиции — без рывка
        Vector3 end   = data.endPos + Vector3.up * agent.baseOffset;

        float duration = Mathf.Max(Vector3.Distance(start, end) / jumpSpeed, minJumpDuration);

        // Поворот лицом к точке приземления
        Vector3 flatDir = end - start;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(flatDir);

        if (animator && !string.IsNullOrEmpty(jumpTrigger))
            animator.SetTrigger(jumpTrigger);

        float t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / duration;
            float p = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(start, end, p);
            pos.y += 4f * jumpArc * p * (1f - p); // парабола: 0 на концах, пик в середине
            transform.position = pos;
            yield return null;
        }

        transform.position = end;
        agent.CompleteOffMeshLink(); // агент снова на NavMesh целевого острова

        if (animator && !string.IsNullOrEmpty(landTrigger))
            animator.SetTrigger(landTrigger);

        _traversing = false;
    }
}
