using UnityEngine;

[ExecuteAlways]
public class SecurityCameraSensor : MonoBehaviour
{
    [Header("View")]
    [Tooltip("Maximum range in meters.")]
    [Min(0.01f)]
    public float detectionRange = 8f;

    [Tooltip("Half-angle of the camera cone in degrees.")]
    [Range(1f, 89f)]
    public float halfFov = 35f;

    [Header("Target")]
    [Tooltip("Collider used for detection (assign the player's main collider).")]
    public Collider playerCollider;

    [Header("Occlusion")]

    [Tooltip("Layers that can block line-of-sight raycasts.")]
    public LayerMask obstructionLayers;

    [Header("State")]
    [Tooltip("Pause detection without disabling the object.")]
    public bool blocked = false;

    [Header("Gizmos")]
    [Tooltip("Show detection cone gizmo in the scene.")]
    public bool showGizmos = true;

    [Tooltip("Keep gizmo visible even when this object is not selected.")]
    public bool persistentGizmo = true;

    public bool IsDetected { get; private set; }

    private void Awake()
    {
        playerCollider = GameObject.FindWithTag("Player").GetComponent<Collider>();
        if (playerCollider == null)
            Debug.Log($"Womp Womp");
    }

    private void OnValidate()
    {
        detectionRange = Mathf.Max(0.01f, detectionRange);
        halfFov = Mathf.Clamp(halfFov, 1f, 89f);
    }

    private void Update()
    {
        IsDetected = !blocked && HasVisibleTarget();
    }

    private bool HasVisibleTarget()
    {
        if (playerCollider == null)
            return false;

        if (!playerCollider.gameObject.activeInHierarchy)
            return false;

        Vector3 origin = transform.position;
        Vector3 closestPoint = playerCollider.ClosestPoint(origin);
        Vector3 toClosest = closestPoint - origin;

        if (toClosest.sqrMagnitude > detectionRange * detectionRange)
            return false;

        return IsTargetVisible(playerCollider, closestPoint);
    }

    private bool IsTargetVisible(Collider target, Vector3 closestPoint)
    {
        Vector3 origin = transform.position;
        Bounds bounds = target.bounds;
        Vector3 targetPoint = closestPoint;

        if (IsPointVisible(origin, targetPoint))
            return true;

        targetPoint = bounds.center;
        if (IsPointVisible(origin, targetPoint))
            return true;

        float halfHeight = bounds.extents.y;
        if (halfHeight > 0.001f)
        {
            Vector3 upperPoint = bounds.center + Vector3.up * halfHeight;
            if (IsPointVisible(origin, upperPoint))
                return true;

            Vector3 lowerPoint = bounds.center - Vector3.up * halfHeight;
            if (IsPointVisible(origin, lowerPoint))
                return true;
        }

        return false;
    }

    private bool IsPointVisible(Vector3 origin, Vector3 targetPoint)
    {
        Vector3 toTarget = targetPoint - origin;

        float sqrDistance = toTarget.sqrMagnitude;
        if (sqrDistance > detectionRange * detectionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > halfFov)
            return false;

        float distance = Mathf.Sqrt(sqrDistance);
        Vector3 direction = toTarget / distance;

        if (Physics.Raycast(origin, direction, distance, obstructionLayers, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || !persistentGizmo)
            return;

        DrawDetectionGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || persistentGizmo)
            return;

        DrawDetectionGizmo();
    }

    private void DrawDetectionGizmo()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 up = transform.up;
        Vector3 right = transform.right;

        Gizmos.color = (IsDetected && !blocked) ? Color.red : Color.yellow;

        float coneBaseRadius = Mathf.Tan(halfFov * Mathf.Deg2Rad) * detectionRange;
        Vector3 coneCenter = origin + forward * detectionRange;

        Vector3 top = coneCenter + up * coneBaseRadius;
        Vector3 bottom = coneCenter - up * coneBaseRadius;
        Vector3 coneRight = coneCenter + right * coneBaseRadius;
        Vector3 coneLeft = coneCenter - right * coneBaseRadius;

        Gizmos.DrawLine(origin, top);
        Gizmos.DrawLine(origin, bottom);
        Gizmos.DrawLine(origin, coneRight);
        Gizmos.DrawLine(origin, coneLeft);

        const int circleSegments = 64;
        const int sliceCount = 4;

        for (int slice = 1; slice <= sliceCount; slice++)
        {
            float sliceT = slice / (float)sliceCount;
            float sliceDistance = detectionRange * sliceT;
            float sliceRadius = coneBaseRadius * sliceT;
            Vector3 sliceCenter = origin + forward * sliceDistance;

            Vector3 previousPoint = sliceCenter + right * sliceRadius;
            for (int i = 1; i <= circleSegments; i++)
            {
                float t = i / (float)circleSegments;
                float radians = t * Mathf.PI * 2f;
                Vector3 pointOnCircle = sliceCenter
                    + right * Mathf.Cos(radians) * sliceRadius
                    + up * Mathf.Sin(radians) * sliceRadius;

                Gizmos.DrawLine(previousPoint, pointOnCircle);
                previousPoint = pointOnCircle;
            }
        }
    }
}
