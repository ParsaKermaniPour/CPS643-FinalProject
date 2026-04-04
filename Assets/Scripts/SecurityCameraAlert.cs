using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Security camera alert system.
/// - Monitors a SecurityCameraSensor for sustained player detection.
/// - After a configurable threshold (default 3 seconds), alerts the closest available robot to investigate.
/// - Robots are found automatically from a container GameObject (default: "SecurityRobots").
/// - Each robot can only handle one investigation at a time.
/// </summary>
public class SecurityCameraAlert : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private SecurityCameraSensor cameraSensor;
    [SerializeField, Min(0.1f)] private float alertThreshold = 3f;

    [Header("Robot Container")]
    [SerializeField] private string robotContainerName = "SecurityRobots";

    private RobotPatrolWalker[] robots;
    private float alertTimer;
    private bool hasAlertedThisDetection;

    private void Start()
    {
        // Auto-find robot container and cache all robot components
        GameObject robotContainer = GameObject.Find(robotContainerName);
        if (robotContainer == null)
        {
            Debug.LogWarning($"SecurityCameraAlert: Could not find GameObject '{robotContainerName}'. Robot alerting disabled.");
            robots = new RobotPatrolWalker[0];
            return;
        }

        robots = robotContainer.GetComponentsInChildren<RobotPatrolWalker>();
        if (robots.Length == 0)
        {
            Debug.LogWarning($"SecurityCameraAlert: No RobotPatrolWalker components found in '{robotContainerName}' hierarchy.");
        }
    }

    private void Update()
    {
        if (cameraSensor == null || robots.Length == 0)
            return;

        bool isDetected = cameraSensor.IsDetected;

        if (isDetected && !hasAlertedThisDetection)
        {
            alertTimer += Time.deltaTime;

            if (alertTimer >= alertThreshold)
            {
                AlertClosestPatrollingRobot();
                hasAlertedThisDetection = true;
            }
        }
        else if (!isDetected)
        {
            alertTimer = 0f;
            hasAlertedThisDetection = false;
        }
    }

    private void AlertClosestPatrollingRobot()
    {
        // Find the closest robot that is currently patrolling
        RobotPatrolWalker closestRobot = null;
        float closestDistance = float.MaxValue;

        foreach (RobotPatrolWalker robot in robots)
        {
            if (robot == null)
                continue;

            if (!robot.IsPatrolling)
                continue;

            float distance = Vector3.Distance(transform.position, robot.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRobot = robot;
            }
        }

        if (closestRobot == null)
            return;

        // Get player's last known position projected onto NavMesh
        if (cameraSensor.playerCollider == null)
            return;

        Vector3 playerWorldPos = cameraSensor.playerCollider.gameObject.transform.position;
        Vector3 investigationPos = playerWorldPos;

        if (NavMesh.SamplePosition(playerWorldPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            investigationPos = hit.position;
        }

        // Alert the robot to investigate
        closestRobot.BeginInvestigation(investigationPos, this);
    }

    public void RobotFinishedInvestigation()
    {
        // Called by robot when investigation completes; allows camera to alert again if needed
        hasAlertedThisDetection = false;
        alertTimer = 0f;
    }
}
