using UnityEngine;
using UnityEngine.AI;

public enum GuardState { Patrol, Chase, Search }

[RequireComponent(typeof(NavMeshAgent))]
public class NPCBrain : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCSensor sensor;
    [SerializeField] private Transform[] patrolPoints;

    [Header("Parameter AI")]
    [SerializeField] [Min(0f)] private float searchDuration = 5f;
    [SerializeField] [Min(0f)] private float arrivalThreshold = 0.5f;
    [SerializeField] [Min(0f)] private float patrolSpeed = 3.5f;
    [SerializeField] [Min(0f)] private float chaseSpeed = 6f;

    [Header("Debug (read only)")]
    [SerializeField] private GuardState currentState = GuardState.Patrol;
    [SerializeField] private int currentPatrolIndex;

    private Vector3 lastKnownPosition;
    private bool hasLastKnownPosition;
    private float searchTimer;
    private NavMeshAgent agent;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (sensor == null) sensor = GetComponentInChildren<NPCSensor>();
        if (sensor == null) Debug.LogError("[NPCBrain] No NPCSensor assigned.", this);
    }

    private void Start()
    {
        SetState(GuardState.Patrol);
    }

    private void Update()
    {
        if (sensor == null) return;
        UpdateMemory();
        Decide();
        Act();
    }

    private void UpdateMemory()
    {
        if (sensor.CanSeePlayer)
        {
            lastKnownPosition = sensor.PlayerPosition;
            hasLastKnownPosition = true;
        }

        if (currentState == GuardState.Search)
        {
            searchTimer -= Time.deltaTime;
        }
    }

    private void Decide()
    {
        if (sensor.CanSeePlayer)
        {
            if (currentState != GuardState.Chase) SetState(GuardState.Chase);
            return;
        }

        if (currentState == GuardState.Chase && hasLastKnownPosition)
        {
            SetState(GuardState.Search);
            return;
        }

        if (currentState == GuardState.Search)
        {
            bool arrivedAtLastKnown = !agent.pathPending && agent.remainingDistance <= arrivalThreshold;
            bool timedOut = searchTimer <= 0f;

            if (timedOut && arrivedAtLastKnown)
            {
                hasLastKnownPosition = false;
                SetState(GuardState.Patrol);
            }
        }
    }

    private void Act()
    {
        switch (currentState)
        {
            case GuardState.Patrol: ActPatrol(); break;
            case GuardState.Chase: ActChase(); break;
            case GuardState.Search: break; 
        }
    }

    private void ActPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        bool arrived = !agent.pathPending && agent.remainingDistance <= arrivalThreshold;
        if (arrived)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    private void ActChase()
    {
        agent.SetDestination(sensor.PlayerPosition);
    }

    private void SetState(GuardState newState)
    {
        currentState = newState;
        switch (newState)
        {
            case GuardState.Patrol:
                agent.speed = patrolSpeed;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                break;
            case GuardState.Chase:
                agent.speed = chaseSpeed;
                agent.SetDestination(sensor.PlayerPosition);
                break;
            case GuardState.Search:
                agent.speed = patrolSpeed;
                searchTimer = searchDuration;
                if (hasLastKnownPosition) agent.SetDestination(lastKnownPosition);
                break;
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.2f, currentState.ToString());
#endif
        if (hasLastKnownPosition)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastKnownPosition, 0.3f);
            Gizmos.DrawLine(transform.position, lastKnownPosition);
        }

        if (patrolPoints != null && patrolPoints.Length > 1)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next == null) continue;
                Gizmos.DrawLine(patrolPoints[i].position, next.position);
            }
        }
    }
}