using UnityEngine;
using UnityEngine.Events;

public class DialInteractable : VRGrabbableBase
{
    [Header("Dial Settings")]
    [Tooltip("Number of discrete tick values on the dial (e.g. 100 for a 0-99 combination lock)")]
    public int totalNumbers = 100;

    [Tooltip("Snap the dial to the nearest tick when released")]
    public bool snapOnRelease = true;

    [Tooltip("How fast rotation follows the controller (1 = exact, lower = lagged/smooth)")]
    [Range(0.1f, 1f)]
    public float rotationSmoothing = 0.8f;

    [Header("Haptics")]
    [Tooltip("Vibration strength on each tick (0 = off, 1 = full)")]
    [Range(0f, 1f)]
    public float hapticAmplitude = 0.3f;

    [Tooltip("Duration of each haptic pulse in seconds")]
    public float hapticDuration = 0.04f;

    [Header("Events")]
    [Tooltip("Fires every time the dial's number value changes")]
    public UnityEvent<int> onNumberChanged;

    [Tooltip("Fires each frame while held, passing the current raw angle (0-360)")]
    public UnityEvent<float> onAngleChanged;

    [Tooltip("Fires each frame while held, passing the rotation delta (positive = clockwise, negative = counter-clockwise)")]
    public UnityEvent<float> onDeltaChanged;

    public int CurrentNumber { get; private set; } = 0;
    public float CurrentAngle { get; private set; } = 0f;

    private float previousControllerAngle;
    private float targetDialRotation;
    private float displayedDialRotation;
    private float accumulatedRotation;
    private int lastReportedTick;
    private Quaternion initialLocalRotation;

    protected override void Awake()
    {
        base.Awake();
        initialLocalRotation = transform.localRotation;
        CurrentAngle = 0f;
        accumulatedRotation = 0f;
        targetDialRotation = 0f;
        displayedDialRotation = 0f;
        lastReportedTick = 0;
        CurrentNumber = 0;
    }

    protected override void OnGrabStart()
    {
        if (grabbingControllerTransform == null) return;
        previousControllerAngle = GetControllerAngleAroundDial();
    }

    protected override void OnGrabUpdate()
    {
        if (grabbingControllerTransform == null) return;

        float currentControllerAngle = GetControllerAngleAroundDial();
        float delta = Mathf.DeltaAngle(previousControllerAngle, currentControllerAngle);
        previousControllerAngle = currentControllerAngle;

        targetDialRotation += delta;
        accumulatedRotation += delta;

        displayedDialRotation = Mathf.Lerp(displayedDialRotation, targetDialRotation, rotationSmoothing);
        ApplyDialRotation(displayedDialRotation);

        CurrentAngle = NormalizeAngle(accumulatedRotation);
        onAngleChanged?.Invoke(CurrentAngle);
        onDeltaChanged?.Invoke(delta);

        UpdateCurrentNumber();
    }

    protected override void OnGrabEnd()
    {
        if (snapOnRelease)
            SnapToNearestTick();
    }

    private float GetControllerAngleAroundDial()
    {
        if (grabbingControllerTransform == null)
            return previousControllerAngle;

        Vector3 dialAxis = GetDialAxisWorld();
        Vector3 toController = grabbingControllerTransform.position - transform.position;
        Vector3 controllerOnPlane = Vector3.ProjectOnPlane(toController, dialAxis);

        if (controllerOnPlane.sqrMagnitude < 0.000001f)
            return previousControllerAngle;

        controllerOnPlane.Normalize();

        Vector3 reference = GetReferenceDirectionOnDialPlane(dialAxis);
        if (reference.sqrMagnitude < 0.000001f)
            return previousControllerAngle;

        return Vector3.SignedAngle(reference, controllerOnPlane, dialAxis);
    }

    private void UpdateCurrentNumber()
    {
        int currentTick = Mathf.FloorToInt(accumulatedRotation / GetDegreesPerTick());
        if (currentTick == lastReportedTick)
            return;

        int direction = currentTick > lastReportedTick ? 1 : -1;

        while (lastReportedTick != currentTick)
        {
            lastReportedTick += direction;
            CurrentNumber = PositiveModulo(lastReportedTick, totalNumbers);
            onNumberChanged?.Invoke(CurrentNumber);
            FireHapticTick();
        }
    }

    private void SnapToNearestTick()
    {
        float degreesPerTick = GetDegreesPerTick();
        int snappedTick = Mathf.RoundToInt(accumulatedRotation / degreesPerTick);

        accumulatedRotation = snappedTick * degreesPerTick;
        targetDialRotation = accumulatedRotation;
        displayedDialRotation = accumulatedRotation;

        float snappedAngle = NormalizeAngle(accumulatedRotation);
        ApplyDialRotation(displayedDialRotation);

        CurrentAngle = snappedAngle;
        UpdateCurrentNumber();
    }

    private void ApplyDialRotation(float angle)
    {
        transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(angle, Vector3.up);
    }

    private Vector3 GetDialAxisWorld()
    {
        return transform.TransformDirection(Vector3.up).normalized;
    }

    private Vector3 GetReferenceDirectionOnDialPlane(Vector3 axis)
    {
        Vector3 reference = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.forward), axis);

        if (reference.sqrMagnitude < 0.000001f)
            reference = Vector3.ProjectOnPlane(transform.TransformDirection(Vector3.right), axis);

        return reference.normalized;
    }

    private float GetDegreesPerTick()
    {
        return 360f / Mathf.Max(1, totalNumbers);
    }

    private float NormalizeAngle(float angle)
    {
        return ((angle % 360f) + 360f) % 360f;
    }

    private int PositiveModulo(int value, int modulo)
    {
        int safeModulo = Mathf.Max(1, modulo);
        return ((value % safeModulo) + safeModulo) % safeModulo;
    }

    private void FireHapticTick()
    {
        if (hapticAmplitude <= 0f || grabbingController == OVRInput.Controller.None) return;
        OVRInput.SetControllerVibration(1f, hapticAmplitude, grabbingController);
        Invoke(nameof(StopHaptics), hapticDuration);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, grabbingController);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}
