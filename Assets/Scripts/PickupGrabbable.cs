using UnityEngine;

public class PickupGrabbable : VRGrabbableBase
{
    public float followSmoothing = 20f;
    public bool dropWithPhysics = true;

    private Vector3 positionOffset;
    private Quaternion rotationOffset;
    private Rigidbody rb;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
    }

    protected override void OnGrabStart()
    {
        if (grabbingControllerTransform == null) return;

        positionOffset = Quaternion.Inverse(grabbingControllerTransform.rotation) * (transform.position - grabbingControllerTransform.position);
        rotationOffset = Quaternion.Inverse(grabbingControllerTransform.rotation) * transform.rotation;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    protected override void OnGrabUpdate()
    {
        if (grabbingControllerTransform == null) return;

        Vector3 targetPosition = grabbingControllerTransform.position + grabbingControllerTransform.rotation * positionOffset;
        Quaternion targetRotation = grabbingControllerTransform.rotation * rotationOffset;

        float t = Mathf.Clamp01(followSmoothing * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, t);
    }

    protected override void OnGrabEnd()
    {
        if (rb == null) return;

        if (dropWithPhysics)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        else
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}
