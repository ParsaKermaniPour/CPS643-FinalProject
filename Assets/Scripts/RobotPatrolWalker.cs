using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Minimal patrol bot movement.
/// - Walks from waypoint to waypoint on a PatrolRoute.
/// - Stops for a fixed wait duration at each point.
/// - Faces the direction of movement while walking.
/// - Keeps its last facing direction while idling.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotPatrolWalker : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private int startWaypointIndex;

    [Header("Wait Time")]
    [SerializeField, Min(0f)] private float waitTime = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParameter = "IsWalking";

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private float waitTimer;
    private bool waiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.stoppingDistance = 0.15f;
    }

    private void Start()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return;

        currentWaypointIndex = Mathf.Clamp(startWaypointIndex, 0, patrolRoute.Count - 1);
        MoveToCurrentWaypoint();
    }

    private void Update()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return;

        if (waiting)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                waiting = false;
                AdvanceWaypoint();
            }

            return;
        }

        if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
        {
            BeginWait();
        }
    }

    private void MoveToCurrentWaypoint()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return;

        Transform waypoint = patrolRoute.GetWaypoint(currentWaypointIndex);
        if (waypoint == null)
            return;

        agent.isStopped = false;
        agent.SetDestination(waypoint.position);
        SetWalking(true);
    }

    private void AdvanceWaypoint()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return;

        currentWaypointIndex = (currentWaypointIndex + 1) % patrolRoute.Count;

        MoveToCurrentWaypoint();
    }

    private void BeginWait()
    {
        waiting = true;
        waitTimer = waitTime;
        agent.isStopped = true;
        agent.ResetPath();
        SetWalking(false);
    }

    private void SetWalking(bool walking)
    {
        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(walkingParameter))
            return;

        animator.SetBool(walkingParameter, walking);
    }
}
