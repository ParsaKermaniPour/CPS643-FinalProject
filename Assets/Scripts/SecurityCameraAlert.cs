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
    private void OnEnable()
    {
        Debug.Log("[SecurityCameraAlert] OnEnable called");
    }

    private void OnDisable()
    {
        Debug.Log("[SecurityCameraAlert] OnDisable called");
    }

    [Header("Detection")]
    [SerializeField] private SecurityCameraSensor cameraSensor;
    [SerializeField, Min(0.1f)] private float alertThreshold = 3f;

    [Header("Robot Container")]
    [SerializeField] private string robotContainerName = "SecurityRobots";

    [Header("Teleportation (Optional)")]
    [Tooltip("If enabled, player will be teleported to the specified position instead of alerting robots.")]
    [SerializeField] private bool teleportOnAlert = false;
    [Tooltip("Destination to teleport the player to.")]
    [SerializeField] private Vector3 teleportDestination = new Vector3(-22f, 1f, -13f);
    [Tooltip("How long to ignore detection after a teleport, so the player isn't immediately re-caught.")]
    [SerializeField] private float teleportCooldownDuration = 2f;

    private RobotPatrolWalker[] robots;
    private float alertTimer;
    private bool hasAlertedThisDetection;
    private float teleportCooldown;

    private void Start()
    {
        Debug.Log("[SecurityCameraAlert] Start called");

        GameObject robotContainer = GameObject.Find(robotContainerName);
        if (robotContainer == null)
        {
            Debug.LogWarning($"SecurityCameraAlert: Could not find GameObject '{robotContainerName}'. Robot alerting disabled.");
            robots = new RobotPatrolWalker[0];
        }
        else
        {
            robots = robotContainer.GetComponentsInChildren<RobotPatrolWalker>();
            if (robots.Length == 0)
                Debug.LogWarning($"SecurityCameraAlert: No RobotPatrolWalker components found in '{robotContainerName}' hierarchy.");
        }

        Debug.Log("[SecurityCameraAlert] FORCED TEST: Calling TeleportPlayer from Start");
        TeleportPlayer();
    }

    private void Update()
    {
        if (cameraSensor == null)
        {
            Debug.LogWarning("[SecurityCameraAlert] cameraSensor is null in Update");
            return;
        }

        // During cooldown, suppress all detection logic so the player
        // cannot be immediately re-caught after a teleport.
        if (teleportCooldown > 0f)
        {
            teleportCooldown -= Time.deltaTime;
            return;
        }

        bool isDetected = cameraSensor.IsDetected;

        if (isDetected && !hasAlertedThisDetection)
        {
            alertTimer += Time.deltaTime;

            if (alertTimer >= alertThreshold)
            {
                Debug.Log($"[SecurityCameraAlert] Detection threshold reached. teleportOnAlert={teleportOnAlert}");
                if (teleportOnAlert)
                {
                    Debug.Log("[SecurityCameraAlert] teleportOnAlert is TRUE, calling TeleportPlayer()");
                    TeleportPlayer();
                }
                else
                {
                    Debug.Log("[SecurityCameraAlert] teleportOnAlert is FALSE, alerting robots");
                    if (robots.Length > 0)
                        AlertClosestPatrollingRobot();
                }
                hasAlertedThisDetection = true;
            }
        }
        else if (!isDetected)
        {
            alertTimer = 0f;
            hasAlertedThisDetection = false;
        }
    }

    private void TeleportPlayer()
    {
        Debug.Log($"[SecurityCameraAlert] TeleportPlayer called. Destination: {teleportDestination}");

        if (cameraSensor?.playerCollider == null)
        {
            Debug.LogError("[SecurityCameraAlert] playerCollider is null, cannot teleport.");
            return;
        }

        Transform playerRoot = cameraSensor.playerCollider.transform.root;
        Debug.Log($"[SecurityCameraAlert] Teleporting '{playerRoot.name}' from {playerRoot.position} to {teleportDestination}");

        // Zero out velocity on any movement scripts via reflection (covers common field names)
        foreach (MonoBehaviour mb in playerRoot.GetComponents<MonoBehaviour>())
        {
            if (mb == null) continue;

            System.Type type = mb.GetType();
            string[] velocityFieldNames = { "velocity", "_velocity", "moveDirection", "_moveDirection", "currentVelocity", "motion" };

            foreach (string fieldName in velocityFieldNames)
            {
                System.Reflection.FieldInfo field = type.GetField(fieldName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

                if (field != null && field.FieldType == typeof(Vector3))
                {
                    field.SetValue(mb, Vector3.zero);
                    Debug.Log($"[SecurityCameraAlert] Zeroed field '{fieldName}' on {type.Name}");
                }
            }
        }

        // Zero out Rigidbody if present
        Rigidbody rb = playerRoot.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("[SecurityCameraAlert] Zeroed Rigidbody velocity.");
        }

        // Teleport via CharacterController warp or direct position set
        CharacterController cc = playerRoot.GetComponent<CharacterController>();
        if (cc != null)
        {
            Debug.Log("[SecurityCameraAlert] CharacterController found — using warp logic.");
            cc.enabled = false;
            playerRoot.position = teleportDestination;
            cc.enabled = true;
            cc.Move(Vector3.zero);
        }
        else
        {
            playerRoot.position = teleportDestination;
        }

        // Start cooldown AFTER teleport so detection is suppressed while the
        // player settles at the destination and walks away from the camera cone.
        teleportCooldown = teleportCooldownDuration;
        hasAlertedThisDetection = true;
        alertTimer = 0f;

        Debug.Log($"[SecurityCameraAlert] New position: {playerRoot.position}. Cooldown started ({teleportCooldownDuration}s).");
    }

    private void AlertClosestPatrollingRobot()
    {
        RobotPatrolWalker closestRobot = null;
        float closestDistance = float.MaxValue;

        foreach (RobotPatrolWalker robot in robots)
        {
            if (robot == null || !robot.IsPatrolling) continue;

            float distance = Vector3.Distance(transform.position, robot.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestRobot = robot;
            }
        }

        if (closestRobot == null || cameraSensor.playerCollider == null) return;

        Vector3 playerWorldPos = cameraSensor.playerCollider.gameObject.transform.position;
        Vector3 investigationPos = playerWorldPos;

        if (NavMesh.SamplePosition(playerWorldPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            investigationPos = hit.position;

        closestRobot.BeginInvestigation(investigationPos, this);
    }

    public void RobotFinishedInvestigation()
    {
        hasAlertedThisDetection = false;
        alertTimer = 0f;
    }
}