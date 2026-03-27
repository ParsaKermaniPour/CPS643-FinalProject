using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SecurityCameraSensor : MonoBehaviour
{
    [Header("View")]
    [Tooltip("Optional origin for view checks. If empty, this transform is used.")]
    public Transform viewOrigin;

    [Tooltip("Maximum range in meters.")]
    [Min(0.01f)]
    public float detectionRange = 8f;

    [Tooltip("Half-angle of the camera cone in degrees.")]
    [Range(1f, 89f)]
    public float halfFov = 35f;

    [Header("Layer Filtering")]
    [Tooltip("Only colliders on these layers can be detected.")]
    public LayerMask targetLayers;

    [Tooltip("Layers that can block line-of-sight raycasts.")]
    public LayerMask obstructionLayers;

    [Header("Debug")]
    [Tooltip("Prints detection state each frame.")]
    public bool printToConsole = true;

    private readonly HashSet<Collider> candidates = new HashSet<Collider>();
    private SphereCollider triggerCollider;
    public bool IsDetected { get; private set; }

    private void Awake()
    {
        if (viewOrigin == null)
            viewOrigin = transform;

        SyncTriggerCollider();
    }

    private void OnValidate()
    {
        SyncTriggerCollider();
    }

    private void OnEnable()
    {
        IsDetected = false;
        SyncTriggerCollider();
    }

    private void Update()
    {
        IsDetected = HasVisibleTarget();

        if (printToConsole)
            Debug.Log(IsDetected ? "Detected!" : "Not Detected");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLayerInMask(other.gameObject.layer, targetLayers))
            return;

        candidates.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsLayerInMask(other.gameObject.layer, targetLayers))
            return;

        candidates.Remove(other);
    }

    private bool HasVisibleTarget()
    {
        if (candidates.Count == 0)
            return false;

        List<Collider> stale = null;

        foreach (Collider target in candidates)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                if (stale == null)
                    stale = new List<Collider>();
                stale.Add(target);
                continue;
            }

            if (IsTargetVisible(target))
            {
                if (stale != null)
                {
                    for (int i = 0; i < stale.Count; i++)
                        candidates.Remove(stale[i]);
                }
                return true;
            }
        }

        if (stale != null)
        {
            for (int i = 0; i < stale.Count; i++)
                candidates.Remove(stale[i]);
        }

        return false;
    }

    private bool IsTargetVisible(Collider target)
    {
        Vector3 origin = viewOrigin.position;
        Bounds bounds = target.bounds;
        Vector3 targetPoint = target.ClosestPoint(origin);

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

        float angle = Vector3.Angle(viewOrigin.forward, toTarget);
        if (angle > halfFov)
            return false;

        float distance = Mathf.Sqrt(sqrDistance);
        Vector3 direction = toTarget / distance;

        if (Physics.Raycast(origin, direction, distance, obstructionLayers, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private static bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void SyncTriggerCollider()
    {
        if (triggerCollider == null)
            triggerCollider = GetComponent<SphereCollider>();

        if (triggerCollider == null)
            return;

        triggerCollider.isTrigger = true;
        triggerCollider.radius = detectionRange;
    }

    private void OnDrawGizmosSelected()
    {
        Transform originTransform = viewOrigin != null ? viewOrigin : transform;
        Vector3 origin = originTransform.position;
        Vector3 forward = originTransform.forward;

        Gizmos.color = IsDetected ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(origin, detectionRange);

        Quaternion left = Quaternion.AngleAxis(-halfFov, originTransform.up);
        Quaternion right = Quaternion.AngleAxis(halfFov, originTransform.up);

        Gizmos.DrawLine(origin, origin + left * forward * detectionRange);
        Gizmos.DrawLine(origin, origin + right * forward * detectionRange);
    }
}
