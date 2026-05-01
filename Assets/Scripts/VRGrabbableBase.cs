using UnityEngine;

public abstract class VRGrabbableBase : MonoBehaviour
{
    [Header("Grab Detection")]
    [Tooltip("How close the controller must be to grab this object (meters)")]
    public float grabRadius = 0.15f;

    [Tooltip("Visual highlight when the controller is inside grab range")]
    public GameObject highlightObject;

    protected OVRInput.Controller grabbingController = OVRInput.Controller.None;
    protected Transform grabbingControllerTransform;

    private bool leftInRange = false;
    private bool rightInRange = false;
    private SphereCollider grabZone;

    private static VRGrabbableBase leftControllerOwner;
    private static VRGrabbableBase rightControllerOwner;

    protected virtual void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

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
        bool leftGripDown  = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);
        bool rightGripDown = OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);

        if (grabbingController == OVRInput.Controller.None)
        {
            // Not holding - click to grab
            if (leftInRange && leftGripDown && CanStartGrab(OVRInput.Controller.LTouch))
                StartGrab(OVRInput.Controller.LTouch);
            else if (rightInRange && rightGripDown && CanStartGrab(OVRInput.Controller.RTouch))
                StartGrab(OVRInput.Controller.RTouch);
        }
        else
        {
            // Already holding - click again to drop
            bool clickedAgain = (grabbingController == OVRInput.Controller.LTouch && leftGripDown)
                             || (grabbingController == OVRInput.Controller.RTouch && rightGripDown);
            if (clickedAgain)
                EndGrab();
        }
    }

    private void StartGrab(OVRInput.Controller controller)
    {
        grabbingController = controller;
        grabbingControllerTransform = GetControllerTransform(controller);
        SetControllerOwner(controller, this);

        if (highlightObject != null)
            highlightObject.SetActive(false);

        OnGrabStart();
    }

    private void EndGrab()
    {
        OnGrabEnd();
        ClearControllerOwner(grabbingController, this);
        grabbingController = OVRInput.Controller.None;
        grabbingControllerTransform = null;
    }

    protected virtual void OnDisable()
    {
        if (grabbingController != OVRInput.Controller.None)
        {
            ClearControllerOwner(grabbingController, this);
            grabbingController = OVRInput.Controller.None;
            grabbingControllerTransform = null;
        }
        else
        {
            ClearControllerOwner(OVRInput.Controller.LTouch, this);
            ClearControllerOwner(OVRInput.Controller.RTouch, this);
        }
    }

    private bool CanStartGrab(OVRInput.Controller controller)
    {
        VRGrabbableBase owner = GetControllerOwner(controller);
        return owner == null || owner == this;
    }

    private static VRGrabbableBase GetControllerOwner(OVRInput.Controller controller)
    {
        if (controller == OVRInput.Controller.LTouch)
            return leftControllerOwner;
        if (controller == OVRInput.Controller.RTouch)
            return rightControllerOwner;
        return null;
    }

    private static void SetControllerOwner(OVRInput.Controller controller, VRGrabbableBase owner)
    {
        if (controller == OVRInput.Controller.LTouch)
            leftControllerOwner = owner;
        else if (controller == OVRInput.Controller.RTouch)
            rightControllerOwner = owner;
    }

    private static void ClearControllerOwner(OVRInput.Controller controller, VRGrabbableBase owner)
    {
        if (controller == OVRInput.Controller.LTouch && leftControllerOwner == owner)
            leftControllerOwner = null;
        else if (controller == OVRInput.Controller.RTouch && rightControllerOwner == owner)
            rightControllerOwner = null;
    }

    private Transform GetControllerTransform(OVRInput.Controller controller)
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null) return null;

        if (controller == OVRInput.Controller.LTouch)
            return rig.leftHandAnchor;
        else
            return rig.rightHandAnchor;
    }

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

    protected abstract void OnGrabStart();
    protected abstract void OnGrabUpdate();
    protected abstract void OnGrabEnd();

    public bool IsGrabbed => grabbingController != OVRInput.Controller.None;
}