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
    private float targetYRotation;
    private float accumulatedRotation;

    protected override void Awake()
    {
        base.Awake();
        targetYRotation = 0f;
        CurrentAngle = 0f;
        accumulatedRotation = 0f;
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

        targetYRotation += delta;
        accumulatedRotation += delta;

        float newY = Mathf.LerpAngle(transform.eulerAngles.y, targetYRotation, rotationSmoothing);
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, newY, transform.eulerAngles.z);

        CurrentAngle = ((accumulatedRotation % 360f) + 360f) % 360f;
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
        Vector3 toController = grabbingControllerTransform.position - transform.position;
        toController.y = 0;
        toController.Normalize();
        return Mathf.Atan2(toController.x, toController.z) * Mathf.Rad2Deg;
    }

    private void UpdateCurrentNumber()
    {
        int newNumber = Mathf.RoundToInt(CurrentAngle / 360f * totalNumbers) % totalNumbers;

        if (newNumber != CurrentNumber)
        {
            CurrentNumber = newNumber;
            onNumberChanged?.Invoke(CurrentNumber);
            FireHapticTick();
        }
    }

    private void SnapToNearestTick()
    {
        float degreesPerTick = 360f / totalNumbers;
        float snappedAngle = Mathf.Round(CurrentAngle / degreesPerTick) * degreesPerTick;
        targetYRotation = snappedAngle;

        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, snappedAngle, transform.eulerAngles.z);

        CurrentAngle = snappedAngle;
        UpdateCurrentNumber();
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
