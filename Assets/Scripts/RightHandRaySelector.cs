using UnityEngine;

/// <summary>
/// Casts a ray from the right-hand controller and clicks targets with A button.
/// Attach this to an object under OVRCameraRig/TrackingSpace/RightHandAnchor.
/// </summary>
public class RightHandRaySelector : MonoBehaviour
{
    [Header("Origin")]
    [Tooltip("Optional transform used as ray start and direction. If empty, this object's transform is used.")]
    public Transform rayOrigin;

    [Header("Ray")]
    [Tooltip("Maximum ray distance in meters")]
    public float maxDistance = 10f;

    [Tooltip("Layers the ray can hit")]
    public LayerMask interactionMask = ~0;

    [Header("Visual")]
    [Tooltip("LineRenderer used to draw the ray")]
    public LineRenderer lineRenderer;

    public Color idleColor = Color.white;
    public Color hoverColor = Color.green;

    private RayClickable hoveredClickable;
    private Collider hoveredCollider;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.005f;
            lineRenderer.endWidth = 0.005f;
            lineRenderer.useWorldSpace = true;
        }
    }

    private void Update()
    {
        Transform origin = rayOrigin != null ? rayOrigin : transform;
        Ray ray = new Ray(origin.position, origin.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactionMask, QueryTriggerInteraction.Collide);

        Vector3 endPoint = hasHit ? hit.point : ray.origin + ray.direction * maxDistance;
        SetRayVisual(ray.origin, endPoint, hasHit ? hoverColor : idleColor);

        UpdateHoverState(hasHit ? hit.collider : null);

        if (hasHit && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            Click(hit.collider);
    }

    private void OnDisable()
    {
        UpdateHoverState(null);
    }

    private void SetRayVisual(Vector3 start, Vector3 end, Color color)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }

    private void UpdateHoverState(Collider newCollider)
    {
        if (hoveredCollider == newCollider)
            return;

        if (hoveredClickable != null)
            hoveredClickable.OnRayHoverExit();

        hoveredCollider = newCollider;
        hoveredClickable = null;

        if (hoveredCollider == null)
            return;

        hoveredClickable = hoveredCollider.GetComponentInParent<RayClickable>();
        if (hoveredClickable != null)
            hoveredClickable.OnRayHoverEnter();
    }

    private void Click(Collider target)
    {
        RayClickable clickable = target.GetComponentInParent<RayClickable>();
        if (clickable != null)
        {
            clickable.Press();
            return;
        }

        target.gameObject.SendMessageUpwards("OnRayClicked", SendMessageOptions.DontRequireReceiver);
        target.gameObject.SendMessage("OnRayClicked", SendMessageOptions.DontRequireReceiver);
    }
}
