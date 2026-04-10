using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TouchOpenDoorButton : Interactable
{
    [Header("Door Target")]
    public SafeDoorAutoOpen targetDoor;

    [Header("Optional Puzzle Reset")]
    public PuzzleReset puzzleReset;

    [Header("Input")]
    public bool requireGrip = false;
    public bool oneShot = true;
    public float cooldownSeconds = 0.25f;

    [Header("Haptics")]
    public bool haptics = true;
    public float hapticAmplitude = 0.35f;
    public float hapticDuration = 0.03f;

    private float nextAllowedTime;
    private bool hasTriggered;

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    public override void OnTouchEnter(OVRController ctrl)
    {
        if (!requireGrip)
            TryTrigger(ctrl);
    }

    public override void OnTouchStay(OVRController ctrl)
    {
        if (requireGrip && IsGripHeld(ctrl))
            TryTrigger(ctrl);
    }

    private bool IsGripHeld(OVRController ctrl)
    {
        OVRInput.Controller controller =
            ctrl.hand == OVRController.Hand.Left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;

        return OVRInput.Get(ctrl.gripButton, controller);
    }

    private void TryTrigger(OVRController ctrl)
    {
        if (targetDoor == null && puzzleReset == null)
        {
            Debug.LogWarning("TouchOpenDoorButton: assign targetDoor and/or puzzleReset.", this);
            return;
        }

        if (oneShot && hasTriggered)
            return;

        if (Time.time < nextAllowedTime)
            return;

        if (targetDoor != null)
            targetDoor.OpenDoor();

        if (puzzleReset != null)
            puzzleReset.ApplyPuzzleReset();

        hasTriggered = true;
        nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);

        if (haptics)
            ctrl.HapticClick(hapticAmplitude, hapticDuration);
    }

    [ContextMenu("Reset Trigger State")]
    public void ResetTriggerState()
    {
        hasTriggered = false;
        nextAllowedTime = 0f;
    }
}
