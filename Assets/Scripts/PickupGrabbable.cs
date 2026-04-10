using UnityEngine;

public class PickupGrabbable : VRGrabbableBase
{
    public float followSmoothing = 20f;
    public bool dropWithPhysics = true;
    public bool disableCollisionsWhileHeld = true;

    private Vector3 positionOffset;
    private Quaternion rotationOffset;
    private Rigidbody rb;
    private Collider[] cachedColliders;
    private bool[] cachedIsTriggerStates;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();

        cachedColliders = GetComponentsInChildren<Collider>(true);
        cachedIsTriggerStates = new bool[cachedColliders.Length];
        for (int i = 0; i < cachedColliders.Length; i++)
            cachedIsTriggerStates[i] = cachedColliders[i] != null && cachedColliders[i].isTrigger;
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

        SetCollidersTriggerState(true);
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

        SetCollidersTriggerState(false);
    }

    private void SetCollidersTriggerState(bool holding)
    {
        if (!disableCollisionsWhileHeld || cachedColliders == null || cachedIsTriggerStates == null)
            return;

        int count = Mathf.Min(cachedColliders.Length, cachedIsTriggerStates.Length);
        for (int i = 0; i < count; i++)
        {
            Collider col = cachedColliders[i];
            if (col == null)
                continue;

            col.isTrigger = holding ? true : cachedIsTriggerStates[i];
        }
    }

    protected override void OnDisable()
    {
        SetCollidersTriggerState(false);
        base.OnDisable();
    }
}
