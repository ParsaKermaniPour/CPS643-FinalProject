using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Rotates a dial in place when the player grabs and turns it with a controller.
/// Extends VRGrabbableBase — all grab/release detection is handled there.
///
/// The dial spins ONLY around its Y axis; position never changes.
///
/// Assign this to the Dial GameObject (the parent that should rotate).
/// </summary>
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

    // Current displayed number (0 to totalNumbers-1)
    public int CurrentNumber { get; private set; } = 0;

    // Current angle in 0-360 range
    public float CurrentAngle { get; private set; } = 0f;

    private float previousControllerAngle;
    private float targetYRotation;

    protected override void Awake()
    {
        base.Awake();
        targetYRotation = transform.eulerAngles.y;
        CurrentAngle = targetYRotation;
    }

    // Called once when player grips the dial
    protected override void OnGrabStart()
    {
        if (grabbingControllerTransform == null) return;

        // Record the initial controller angle relative to the dial center
        previousControllerAngle = GetControllerAngleAroundDial();
    }

    // Called every frame while held — this is where the spinning happens
    protected override void OnGrabUpdate()
    {
        if (grabbingControllerTransform == null) return;

        float currentControllerAngle = GetControllerAngleAroundDial();

        // Calculate how much the controller moved angularly since last frame
        float delta = Mathf.DeltaAngle(previousControllerAngle, currentControllerAngle);
        previousControllerAngle = currentControllerAngle;

        // Apply delta to our target rotation
        targetYRotation += delta;

        // Smooth toward target (or set directly if smoothing == 1)
        float newY = Mathf.LerpAngle(transform.eulerAngles.y, targetYRotation, rotationSmoothing);

        // Only rotate Y — keep X and Z locked so dial stays upright
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, newY, transform.eulerAngles.z);

        // Normalize angle to 0-360 range
        CurrentAngle = (transform.eulerAngles.y % 360f + 360f) % 360f;
        onAngleChanged?.Invoke(CurrentAngle);
        onDeltaChanged?.Invoke(delta);

        // Map angle to a number on the dial
        UpdateCurrentNumber();
    }

    // Called once when player releases grip
    protected override void OnGrabEnd()
    {
        if (snapOnRelease)
        {
            SnapToNearestTick();
        }
    }

    // ---- Angle calculation ----

    /// <summary>
    /// Returns the angle (degrees) from the dial center to the controller,
    /// projected onto the horizontal plane. This is what drives the rotation.
    /// </summary>
    private float GetControllerAngleAroundDial()
    {
        // Get vector from dial center to controller, flattened to XZ plane
        Vector3 toController = grabbingControllerTransform.position - transform.position;
        toController.y = 0;
        toController.Normalize();

        // Atan2 gives angle in degrees from the forward axis, going clockwise
        return Mathf.Atan2(toController.x, toController.z) * Mathf.Rad2Deg;
    }

    // ---- Number tracking ----

    private void UpdateCurrentNumber()
    {
        // Map the 0-360 dial angle to a tick number (0 to totalNumbers-1)
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
        // Degrees per tick
        float degreesPerTick = 360f / totalNumbers;

        // Round to nearest tick angle
        float snappedAngle = Mathf.Round(CurrentAngle / degreesPerTick) * degreesPerTick;
        targetYRotation = snappedAngle;

        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, snappedAngle, transform.eulerAngles.z);

        CurrentAngle = snappedAngle;
        UpdateCurrentNumber();
    }

    // ---- Haptics ----

    private void FireHapticTick()
    {
        if (hapticAmplitude <= 0f || grabbingController == OVRInput.Controller.None) return;

        // Short sharp vibration — like a physical detent click on a real safe dial
        OVRInput.SetControllerVibration(1f, hapticAmplitude, grabbingController);

        // Stop the vibration after the pulse duration
        Invoke(nameof(StopHaptics), hapticDuration);
    }

    private void StopHaptics()
    {
        OVRInput.SetControllerVibration(0f, 0f, grabbingController);
    }

    // ---- Debug helper ----

    private void OnDrawGizmosSelected()
    {
        // Show the grab zone radius in the editor
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grabRadius);
    }
}
