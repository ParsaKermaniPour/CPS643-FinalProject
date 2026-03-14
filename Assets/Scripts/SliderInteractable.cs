using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A VR-grabbable slider that moves along a single axis when grabbed.
/// Extends VRGrabbableBase — all grab/release detection is handled there.
///
/// PLACEHOLDER — ready to implement when needed.
/// </summary>
public class SliderInteractable : VRGrabbableBase
{
    [Header("Slider Settings")]
    [Tooltip("The axis the slider moves along in local space")]
    public Vector3 slideAxis = Vector3.right;

    [Tooltip("Minimum local position along the slide axis")]
    public float minValue = -0.5f;

    [Tooltip("Maximum local position along the slide axis")]
    public float maxValue = 0.5f;

    [Header("Events")]
    [Tooltip("Fires every frame with a normalized value (0 = min, 1 = max)")]
    public UnityEvent<float> onValueChanged;

    public float CurrentValue { get; private set; } = 0f;

    private Vector3 grabOffset;
    private Vector3 startPosition;

    protected override void OnGrabStart()
    {
        if (grabbingControllerTransform == null) return;

        startPosition = transform.localPosition;
        grabOffset = transform.position - grabbingControllerTransform.position;
    }

    protected override void OnGrabUpdate()
    {
        if (grabbingControllerTransform == null) return;

        // Project controller movement onto slide axis
        Vector3 worldAxis = transform.parent != null
            ? transform.parent.TransformDirection(slideAxis.normalized)
            : slideAxis.normalized;

        Vector3 targetWorldPos = grabbingControllerTransform.position + grabOffset;
        Vector3 toTarget = targetWorldPos - transform.position;
        float dot = Vector3.Dot(toTarget, worldAxis);

        // Clamp the slider within min/max
        Vector3 localPos = transform.localPosition;
        float projected = Vector3.Dot(localPos, slideAxis.normalized) + dot * Time.deltaTime * 10f;
        projected = Mathf.Clamp(projected, minValue, maxValue);

        transform.localPosition = slideAxis.normalized * projected;

        // Normalize to 0-1 range
        CurrentValue = Mathf.InverseLerp(minValue, maxValue, projected);
        onValueChanged?.Invoke(CurrentValue);
    }

    protected override void OnGrabEnd()
    {
        // Optional: snap to nearest value, animate back, etc.
    }
}
