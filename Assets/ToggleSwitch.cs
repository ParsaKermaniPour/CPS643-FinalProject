using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class ToggleSwitch : Interactable
{
    public enum Axis { X, Y, Z }

    [Header("Rotation")]
    public Axis hingeAxis = Axis.X;
    public float offAngle = -25f;
    public float onAngle  = 25f;

    [Header("Motion")]
    public float followSpeed = 25f;
    public float snapSpeed = 14f;

    [Header("State")]
    public bool state;

    [Header("Haptics")]
    public bool haptics = true;
    public float toggleAmplitude = 0.45f;
    public float toggleDuration = 0.035f;

    [Header("Event")]
    public UnityEvent<bool> OnSwitch;

    Quaternion initialLocalRotation;
    OVRController activeController;
    bool isTouching;
    float angle;
    float targetAngle;
    float angleAtTouchStart;
    float controllerAngleAtTouchStart;

    void Awake()
    {
        initialLocalRotation = transform.localRotation;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        angle = state ? onAngle : offAngle;
        targetAngle = angle;
        Apply(angle);
        OnSwitch?.Invoke(state);
    }

    public override void OnTouchEnter(OVRController ctrl)
    {
        isTouching = true;
        activeController = ctrl;
        angleAtTouchStart = angle;
        controllerAngleAtTouchStart = ControllerAngleLocal(ctrl.transform.position);
    }

    public override void OnTouchExit(OVRController ctrl)
    {
        if (ctrl != activeController) return;
        isTouching = false;
    }

    void Update()
    {
        if (isTouching && activeController != null)
        {
            float delta = Mathf.DeltaAngle(controllerAngleAtTouchStart, ControllerAngleLocal(activeController.transform.position));
            targetAngle = Clamp(angleAtTouchStart + delta, offAngle, onAngle);
        }
        else
        {
            float mid = 0.5f * (offAngle + onAngle);
            bool newState = angle >= mid;
            targetAngle = newState ? onAngle : offAngle;

            if (newState != state)
            {
                state = newState;
                OnSwitch?.Invoke(state);
                if (haptics && activeController != null)
                    activeController.HapticClick(toggleAmplitude, toggleDuration);
            }
        }

        float speed = isTouching ? followSpeed : snapSpeed;
        angle = Mathf.LerpAngle(angle, targetAngle, speed * Time.deltaTime);
        Apply(angle);
    }

    void Apply(float a)
        => transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(a, HingeAxis());

    Vector3 HingeAxis()
        => hingeAxis == Axis.X ? Vector3.right
         : hingeAxis == Axis.Y ? Vector3.up
         : Vector3.forward;

    static float Clamp(float a, float lo, float hi)
        => Mathf.Clamp(a, Mathf.Min(lo, hi), Mathf.Max(lo, hi));

    Vector2 PlaneBasis(Vector3 normal, Vector3 projected)
    {
        Vector3 refDir = Mathf.Abs(Vector3.Dot(Vector3.up, normal)) > 0.85f ? Vector3.right : Vector3.up;
        Vector3 u = Vector3.ProjectOnPlane(refDir, normal).normalized;
        Vector3 v = Vector3.Cross(normal, u).normalized;
        return new Vector2(Vector3.Dot(projected, u), Vector3.Dot(projected, v));
    }

    float ControllerAngleLocal(Vector3 ctrlWorldPos)
    {
        Transform p = transform.parent;
        if (p == null) return 0f;

        Vector3 axis = HingeAxis();
        Vector3 projected = Vector3.ProjectOnPlane(
            p.InverseTransformPoint(ctrlWorldPos) - p.InverseTransformPoint(transform.position),
            axis);

        if (projected.sqrMagnitude < 1e-8f) return 0f;

        Vector2 uv = PlaneBasis(axis, projected);
        return Mathf.Atan2(uv.y, uv.x) * Mathf.Rad2Deg;
    }
}