using UnityEngine;

/// <summary>
/// VR Movement using OVRCameraRig + OVRInput (Meta/Oculus SDK)
/// Attach this script to your OVRCameraRig GameObject
/// Left joystick moves you around
/// </summary>
public class VRMovementSimple : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast you move in meters per second")]
    public float moveSpeed = 2.0f;

    [Tooltip("The CenterEyeAnchor inside your OVRCameraRig (the headset camera)")]
    public Transform centerEyeAnchor;

    private CharacterController characterController;

    void Start()
    {
        // Auto-find CenterEyeAnchor if not assigned in Inspector
        if (centerEyeAnchor == null)
        {
            // OVRCameraRig structure: OVRCameraRig → TrackingSpace → CenterEyeAnchor
            Transform trackingSpace = transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                centerEyeAnchor = trackingSpace.Find("CenterEyeAnchor");
            }
        }

        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("VRMovementSimple: Could not find CenterEyeAnchor. Drag it in manually in the Inspector.");
        }

        // Add CharacterController for collision detection
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);
        }
    }

    void Update()
    {
        // Read left thumbstick using OVRInput
        // LThumbstick = left joystick axis (x = left/right, y = forward/back)
        Vector2 joystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (centerEyeAnchor == null) return;

        // Get head-facing direction (flatten to horizontal so you don't fly up)
        Vector3 forward = centerEyeAnchor.forward;
        Vector3 right = centerEyeAnchor.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Build movement vector from joystick input
        Vector3 moveDirection = (forward * joystickInput.y) + (right * joystickInput.x);
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

        // Apply gravity when airborne
        if (!characterController.isGrounded)
        {
            movement.y -= 9.81f * Time.deltaTime;
        }

        characterController.Move(movement);
    }
}
