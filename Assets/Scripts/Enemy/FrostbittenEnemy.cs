using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class FrostbittenEnemy : MonoBehaviour
{
    public enum EnemyState
    {
        Frozen,
        Thawing,
        Active,
        Dead
    }

    public EnemyState CurrentState { get; private set; } = EnemyState.Frozen;

    [SerializeField] private GameManager.SectorID boundSector;
    [SerializeField] private Transform[] patrolWaypoints;

    private NavMeshAgent agent;
    private int currentWaypointIndex = 0;
    private Coroutine thawCoroutine;

    private Color frozenColor = Color.blue;
    private Color thawingColor = Color.yellow;
    private Color activeColor = Color.red;
    private Color deadColor = Color.black;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = false;
    }

    private bool isSubscribed = false;

    private void Start()
    {
        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnSectorPoweredOn += HandleSectorPowered;
            isSubscribed = true;
        }
    }

    private void OnEnable()
    {

        if (GameManager.Instance != null && !isSubscribed)
        {
            GameManager.Instance.OnSectorPoweredOn += HandleSectorPowered;
            isSubscribed = true;
        }
        else if (GameManager.Instance == null)
        {
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null && isSubscribed)
        {
            GameManager.Instance.OnSectorPoweredOn -= HandleSectorPowered;
            isSubscribed = false;
        }
    }

    private void HandleSectorPowered(GameManager.SectorID sector)
    {

        if (sector == boundSector && CurrentState == EnemyState.Frozen)
        {
            SetState(EnemyState.Thawing);
        }
    }

    public void SetState(EnemyState newState)
    {
        if (CurrentState == newState)
            return;

        CurrentState = newState;
        switch (CurrentState)
        {
            case EnemyState.Frozen:
                OnFrozen();
                break;
            case EnemyState.Thawing:
                OnThawing();
                break;
            case EnemyState.Active:
                OnActive();
                break;
            case EnemyState.Dead:
                OnDead();
                break;
        }
    }

    protected virtual void OnThawing()
    {
        if (agent != null)
            agent.enabled = false;

        thawCoroutine = StartCoroutine(WaitAndActivate(3f));
    }

    private IEnumerator WaitAndActivate(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetState(EnemyState.Active);
    }

    protected virtual void OnActive()
    {

        if (agent != null)
        {
            agent.enabled = true;
            PatrolToNextWaypoint();
        }
    }

    private void PatrolToNextWaypoint()
    {
        if (patrolWaypoints == null || patrolWaypoints.Length == 0)
            return;

        if (currentWaypointIndex >= patrolWaypoints.Length)
            currentWaypointIndex = 0;

        Transform targetWaypoint = patrolWaypoints[currentWaypointIndex];
        agent.SetDestination(targetWaypoint.position);
    }

    private void Update()
    {
        if (CurrentState != EnemyState.Active)
            return;

        transform.position = new Vector3(transform.position.x, 1.0f, transform.position.z);

        if (agent.remainingDistance < 0.5f && !agent.pathPending)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
            PatrolToNextWaypoint();
        }
    }

    protected virtual void OnFrozen()
    {
        if (agent != null)
            agent.enabled = false;
    }

    protected virtual void OnDead()
    {
        if (agent != null)
            agent.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GetStateColor();

        Vector3 spherePosition = transform.position + Vector3.up * 2f;
        Gizmos.DrawSphere(spherePosition, 0.3f);
    }

    private Color GetStateColor()
    {
        return CurrentState switch
        {
            EnemyState.Frozen => frozenColor,
            EnemyState.Thawing => thawingColor,
            EnemyState.Active => activeColor,
            EnemyState.Dead => deadColor,
            _ => Color.white
        };
    }
}