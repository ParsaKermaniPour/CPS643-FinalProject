using UnityEngine;

/// <summary>
/// Abstract base class for any VR-interactable object.
/// Handles controller proximity detection and grip press/release.
/// Extend this for Dial, Slider, Button, Lever, etc.
///
/// How it works:
///   - A trigger sphere collider on this object detects when a controller is nearby
///   - When the player presses Grip while a controller is inside that sphere, the grab starts
///   - Subclasses implement OnGrabStart / OnGrabUpdate / OnGrabEnd to define behavior
/// </summary>
public abstract class VRGrabbableBase : MonoBehaviour
{
    [Header("Grab Detection")]
    [Tooltip("How close the controller must be to grab this object (meters)")]
    public float grabRadius = 0.15f;

    [Tooltip("Visual highlight when the controller is inside grab range")]
    public GameObject highlightObject;

    // Currently grabbing controller (None if not grabbed)
    protected OVRInput.Controller grabbingController = OVRInput.Controller.None;

    // Transform of the grabbing controller anchor
    protected Transform grabbingControllerTransform;

    private bool leftInRange = false;
    private bool rightInRange = false;
    private SphereCollider grabZone;

    protected virtual void Awake()
    {
        // Add Rigidbody if missing, then lock it so object never physically moves
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Create an invisible sphere trigger for grab detection
        grabZone = gameObject.AddComponent<SphereCollider>();
        grabZone.isTrigger = true;
        grabZone.radius = grabRadius;

        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    protected virtual void Update()
    {
        HandleGrabInput();

        if (grabbingController != OVRInput.Controller.None)
        {
            OnGrabUpdate();
        }
    }

    private void HandleGrabInput()
    {
        // --- Left controller ---
        bool leftGripDown  = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        bool leftGripUp    = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger,   OVRInput.Controller.LTouch);

        // --- Right controller ---
        bool rightGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        bool rightGripUp   = OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger,   OVRInput.Controller.RTouch);

        // Try to grab
        if (grabbingController == OVRInput.Controller.None)
        {
            if (leftInRange && leftGripDown)
                StartGrab(OVRInput.Controller.LTouch);
            else if (rightInRange && rightGripDown)
                StartGrab(OVRInput.Controller.RTouch);
        }
        // Release if grip is let go
        else
        {
            bool released = (grabbingController == OVRInput.Controller.LTouch && leftGripUp)
                         || (grabbingController == OVRInput.Controller.RTouch && rightGripUp);
            if (released)
                EndGrab();
        }
    }

    private void StartGrab(OVRInput.Controller controller)
    {
        grabbingController = controller;
        grabbingControllerTransform = GetControllerTransform(controller);

        if (highlightObject != null)
            highlightObject.SetActive(false);

        OnGrabStart();
    }

    private void EndGrab()
    {
        OnGrabEnd();
        grabbingController = OVRInput.Controller.None;
        grabbingControllerTransform = null;
    }

    // Returns the world-space transform of the given controller anchor
    private Transform GetControllerTransform(OVRInput.Controller controller)
    {
        // Find OVRCameraRig's tracking space anchors
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null) return null;

        if (controller == OVRInput.Controller.LTouch)
            return rig.leftHandAnchor;
        else
            return rig.rightHandAnchor;
    }

    // ---- Trigger callbacks for detecting controller in range ----

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("LeftController"))
        {
            leftInRange = true;
            ShowHighlight();
        }
        else if (other.CompareTag("RightController"))
        {
            rightInRange = true;
            ShowHighlight();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("LeftController"))
            leftInRange = false;
        else if (other.CompareTag("RightController"))
            rightInRange = false;

        if (!leftInRange && !rightInRange && grabbingController == OVRInput.Controller.None)
            HideHighlight();
    }

    private void ShowHighlight()
    {
        if (highlightObject != null && grabbingController == OVRInput.Controller.None)
            highlightObject.SetActive(true);
    }

    private void HideHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    // ---- Override these in subclasses ----

    /// <summary>Called once when the player grips and grabs this object</summary>
    protected abstract void OnGrabStart();

    /// <summary>Called every frame while the object is held</summary>
    protected abstract void OnGrabUpdate();

    /// <summary>Called once when the player releases the grip</summary>
    protected abstract void OnGrabEnd();

    public bool IsGrabbed => grabbingController != OVRInput.Controller.None;
}
