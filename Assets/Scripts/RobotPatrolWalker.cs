using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Robot patrol and chase behavior.
/// - Walks waypoint-to-waypoint on PatrolRoute when patrolling.
/// - Detects player via SecurityCameraSensor; requires 2 seconds of sustained detection to enter chase.
/// - Chases player using NavMesh pathfinding while detected.
/// - Returns to patrol after 5 seconds of undetection, resuming from closest waypoint.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class RobotPatrolWalker : MonoBehaviour
{
    private enum RobotState { Patrolling, Detecting, Chasing }

    [Header("Route")]
    [SerializeField] private PatrolRoute patrolRoute;
    [SerializeField] private int startWaypointIndex;

    [Header("Wait Time")]
    [SerializeField, Min(0f)] private float waitTime = 1.5f;

    [Header("Movement Speed")]
    [SerializeField, Min(0.1f)] private float patrolSpeed = 1f;
    [SerializeField, Min(0.1f)] private float chaseSpeed = 1.5f;

    [Header("Detection")]
    [SerializeField] private SecurityCameraSensor cameraSensor;
    [SerializeField, Min(0f)] private float detectionThreshold = 2f;
    [SerializeField, Min(0f)] private float escapeThreshold = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkingParameter = "IsWalking";
    [SerializeField] private string chasingParameter = "IsChasing";

    private NavMeshAgent agent;
    private int currentWaypointIndex;
    private float waitTimer;
    private bool waiting;

    private RobotState currentState;
    private float detectionTimer;
    private float lostSightTimer;
    private int waypointIndexBeforeChase;
    private Vector3 chaseTargetPosition;
    private bool wasDetectedLastFrame;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        agent.autoBraking = true;
        agent.updateRotation = true;
        agent.updateUpAxis = true;
        agent.stoppingDistance = 0.15f;
        agent.speed = patrolSpeed;

        currentState = RobotState.Patrolling;
        detectionTimer = 0f;
        lostSightTimer = 0f;
        wasDetectedLastFrame = false;
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

        if (cameraSensor == null)
            return;

        // Detect state transitions based on camera sensor
        bool isDetectedThisFrame = cameraSensor.IsDetected;

        if (isDetectedThisFrame && !wasDetectedLastFrame)
        {
            OnPlayerFirstDetected();
        }
        else if (!isDetectedThisFrame && wasDetectedLastFrame)
        {
            OnPlayerLostSight();
        }

        wasDetectedLastFrame = isDetectedThisFrame;

        // Handle per-state logic
        switch (currentState)
        {
            case RobotState.Patrolling:
                UpdatePatrolling();
                break;

            case RobotState.Detecting:
                UpdateDetecting();
                break;

            case RobotState.Chasing:
                UpdateChasing();
                break;
        }
    }

    private void UpdatePatrolling()
    {
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

    private void UpdateDetecting()
    {
        if (cameraSensor.IsDetected)
        {
            detectionTimer += Time.deltaTime;

            if (detectionTimer >= detectionThreshold)
            {
                EnterChaseMode();
            }
        }
    }

    private void UpdateChasing()
    {
        UpdateChaseTarget();  // Always pursue, even if LOS is broken

        if (cameraSensor.IsDetected)
        {
            lostSightTimer = 0f;  // Reset if we see them again
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= escapeThreshold)
            {
                ExitChaseMode();  // Give up after 5 seconds out of sight
            }
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

    private void SetChasing(bool chasing)
    {
        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(chasingParameter))
            return;

        animator.SetBool(chasingParameter, chasing);
    }

    private void OnPlayerFirstDetected()
    {
        if (currentState == RobotState.Patrolling)
        {
            currentState = RobotState.Detecting;
            detectionTimer = 0f;
        }
        else if (currentState == RobotState.Chasing)
        {
            lostSightTimer = 0f;  // Reset lost sight timer if vision is regained during chase
        }
    }

    private void EnterChaseMode()
    {
        currentState = RobotState.Chasing;
        waypointIndexBeforeChase = currentWaypointIndex;
        detectionTimer = 0f;

        waiting = false;
        waitTimer = 0f;

        agent.speed = chaseSpeed;
        SetChasing(true);
        UpdateChaseTarget();
    }

    private void UpdateChaseTarget()
    {
        if (cameraSensor.playerCollider == null)
            return;

        chaseTargetPosition = cameraSensor.playerCollider.gameObject.transform.position;
        agent.isStopped = false;
        agent.SetDestination(chaseTargetPosition);
    }

    private void OnPlayerLostSight()
    {
        if (currentState == RobotState.Detecting)
        {
            currentState = RobotState.Patrolling;
            detectionTimer = 0f;
        }
        // Chasing state now handled by lostSightTimer in UpdateChasing()
    }

    private void ExitChaseMode()
    {
        currentState = RobotState.Patrolling;
        detectionTimer = 0f;
        lostSightTimer = 0f;

        agent.speed = patrolSpeed;
        SetChasing(false);

        currentWaypointIndex = FindClosestWaypointIndex();
        MoveToCurrentWaypoint();
    }

    private int FindClosestWaypointIndex()
    {
        if (patrolRoute == null || patrolRoute.Count == 0)
            return 0;

        Vector3 robotPosition = transform.position;
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < patrolRoute.Count; i++)
        {
            Transform waypoint = patrolRoute.GetWaypoint(i);
            if (waypoint == null)
                continue;

            float distance = Vector3.Distance(robotPosition, waypoint.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
