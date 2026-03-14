
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class DiegeticSlider : Interactable
{
    public enum LocalAxis { X, Y, Z }

    [Header("Parts")]
    [Tooltip("The moving part (handle). If null, will search for a child named 'Handle'.")]
    public Transform handle;

    [Header("Linear Motion")]
    public LocalAxis axis = LocalAxis.X;

    [Tooltip("Min/max handle position along the axis (in SliderRoot local units).")]
    public float minPos = -0.055f;
    public float maxPos = 0.0075f;

    [Header("Feel")]
    [Tooltip("How quickly the handle follows the controller while gripping (0 = instant).")]
    [Range(0f, 30f)] public float followSpeed = 20f;

    [Tooltip("Ignore tiny controller jitter (in local units along axis).")]
    public float deadZone = 0.0015f;

    [Tooltip("Release grip if controller gets too far from the handle (world units).")]
    public float breakDistance = 0.45f;

    [Header("Haptics (optional)")]
    public bool haptics = true;
    [Range(0f, 1f)] public float dragHaptics = 0.20f;
    public float tickEvery = 0.02f; // how much movement triggers a tick (local units)

    [Header("Output")]
    [Range(0f, 1f)] public float value01;
    public UnityEvent<float> onValueChanged01;

    // --- internal state ---
    OVRController controller;
    float currentPos;      // current handle axis value (local)
    float targetPos;       // desired axis value (local)
    float grabOffset;      // (handlePos - controllerAxis) when grip begins
    float lastTickAt;      // used for optional haptic ticks

    void Awake()
    {
        // Stable trigger/collision behavior
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        if (handle == null) handle = transform.Find("Handle");
        if (handle == null)
        {
            Debug.LogError($"{name}: Slider needs a 'Handle' Transform assigned or a child named 'Handle'.");
            enabled = false;
            return;
        }

        // Initialize to current handle position - use Dot product to project along axis
        currentPos = GetHandlePos();
        targetPos = currentPos;
        Apply(currentPos, forceEvent: true);
    }

    public override void OnGripBegin(OVRController ctrl)
    {
        controller = ctrl;

        // Read the axis values at grip time.
        float handlePos = GetHandlePos(); // Use dot product to project along linear axis of slider
        float cntlrPos = GetControllerLocalPos(ctrl);

        // Store an offset so the handle does NOT snap to the controller.
        // Later: handlePos = cntlrPos + grabOffset
        grabOffset = handlePos - cntlrPos;

        currentPos = handlePos;
        targetPos = handlePos;

        lastTickAt = currentPos;

        if (haptics) controller.HapticClick(0.15f, 0.02f); // small grab tick
    }

    public override void OnGripEnd(OVRController ctrl)
    {
        if (controller != ctrl) return;
        controller = null;
    }

    void Update()
    {
        if (controller == null) return;

        // Break if the hand drifts too far away from the handle.
        if (Vector3.Distance(controller.transform.position, handle.position) > breakDistance)
        {
            if (haptics) controller.HapticClick(0.6f, 0.03f);
            OnGripEnd(controller);
            return;
        }

        // 1) Compute desired handle position from controller position.
        float cntlrPos = GetControllerLocalPos(controller);
        float proposed = cntlrPos + grabOffset;

        // 2) Clamp to slider range.
        proposed = Mathf.Clamp(proposed, Mathf.Min(minPos, maxPos), Mathf.Max(minPos, maxPos));

        // 3) Dead zone: ignore tiny jitter.
        if (Mathf.Abs(proposed - targetPos) >= deadZone)
            targetPos = proposed;

        // 4) Smooth follow (optional).
        // If you want instant, set followSpeed = 0.
        if (followSpeed <= 0f) currentPos = targetPos;
        else currentPos = Mathf.Lerp(currentPos, targetPos, followSpeed * Time.deltaTime);

        // Set handle position
        Apply(currentPos, forceEvent: false);

        // Optional “drag ticks” while sliding
        if (haptics && Mathf.Abs(currentPos - lastTickAt) >= tickEvery)
        {
            lastTickAt = currentPos;
            controller.HapticTick(Mathf.Clamp01(dragHaptics), 0.015f);
        }
    }

    // ---------------- Helper Methods ----------------

    Vector3 AxisLocal()
        => axis == LocalAxis.X ? Vector3.right :
           axis == LocalAxis.Y ? Vector3.up :
                                 Vector3.forward;

    float GetHandlePos()
        => Vector3.Dot(handle.localPosition, AxisLocal());

    float GetControllerLocalPos(OVRController ctrl)
    {
        // Controller position expressed in Slider local space, then projected onto axis.
        Vector3 ctrlLocalPos = transform.InverseTransformPoint(ctrl.transform.position);
        return Vector3.Dot(ctrlLocalPos, AxisLocal());
    }

    void SetHandlePos(float v)
    {
        Vector3 a = AxisLocal();

        // Remove current component along axis, then add the new one.
        Vector3 pos = handle.localPosition;
        pos -= a * Vector3.Dot(pos, a);
        pos += a * v;

        handle.localPosition = pos;
    }

    void Apply(float axisValue, bool forceEvent)
    {
        SetHandlePos(axisValue);

        float t = Mathf.InverseLerp(minPos, maxPos, axisValue);
        if (forceEvent || !Mathf.Approximately(t, value01))
        {
            value01 = t;
            onValueChanged01?.Invoke(value01);
        }
    }
}