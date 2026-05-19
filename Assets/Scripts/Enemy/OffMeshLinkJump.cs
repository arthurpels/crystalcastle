using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class OffMeshLinkJump : MonoBehaviour
{
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float jumpHeight = 1.0f;
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    private NavMeshAgent agent;
    private Animator animator;
    private bool isTraversing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (agent.isOnOffMeshLink && !isTraversing)
        {
            StartCoroutine(TraverseLink());
        }
    }

    IEnumerator TraverseLink()
    {
        isTraversing = true;
        agent.autoTraverseOffMeshLink = false;

        OffMeshLinkData data = agent.currentOffMeshLinkData;
        Vector3 start = agent.transform.position;
        Vector3 end = data.endPos + Vector3.up * agent.baseOffset;

        float dist = Vector3.Distance(start, end);
        bool isJump = Mathf.Abs(start.y - end.y) < 0.5f && dist > 1f;

        if (isJump && animator != null)
            animator.SetTrigger("Jump");

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / jumpDuration;

            Vector3 pos = Vector3.Lerp(start, end, t);
            float height = heightCurve.Evaluate(t) * jumpHeight;
            pos.y += height;

            agent.transform.position = pos;
            yield return null;
        }

        agent.transform.position = end;
        agent.CompleteOffMeshLink();
        agent.autoTraverseOffMeshLink = true;

        if (isJump && animator != null)
            animator.SetTrigger("Land");

        isTraversing = false;
    }
}