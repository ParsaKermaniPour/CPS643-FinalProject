using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class Dial : Interactable
{
    [Header("Range")]
    public float degreeRange = 45f;

    [Header("Feel")]
    [Range(0f, 40f)] public float followSpeed = 25f;
    public float breakDistance = 0.40f;

    [Header("Output")]
    public float angleDeg;
    [Range(0f, 1f)] public float value01;
    public UnityEvent<float> onValueChanged01;

    [Header("Haptics")]
    public bool haptics = true;
    [Range(0f, 1f)] public float dragHaptics = 0.25f;
    [Range(0f, 1f)] public float touchHaptics = 0.25f;

    OVRController activeController;
    Vector3 referenceDir;
    float grabAngle;
    float grabControllerAngle;
    float lastEmitted = -999f;

    void Awake()
    {
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Start()
    {
        angleDeg = Normalize180(transform.localEulerAngles.y);
        if (degreeRange > 0f) angleDeg = Mathf.Clamp(angleDeg, -degreeRange, degreeRange);
        ApplyRotation(angleDeg);
        EmitValue(force: true);
    }

    public override void OnTouchEnter(OVRController c)
    {
        if (haptics) c.HapticTick(touchHaptics, 0.015f);
    }

    public override void OnGripBegin(OVRController c)
    {
        activeController = c;
        referenceDir = c.transform.InverseTransformDirection(transform.right);
        grabAngle = angleDeg;
        grabControllerAngle = GetControllerAngle();
    }

    public override void OnGripEnd(OVRController c)
    {
        if (activeController == c) activeController = null;
    }

    void Update()
    {
        if (activeController == null) return;

        if (Vector3.Distance(activeController.transform.position, transform.position) > breakDistance)
        {
            if (haptics) activeController.HapticClick(0.6f, 0.03f);
            activeController = null;
            return;
        }

        float target = grabAngle + Mathf.DeltaAngle(grabControllerAngle, GetControllerAngle());
        if (degreeRange > 0f) target = Mathf.Clamp(target, -degreeRange, degreeRange);

        angleDeg = followSpeed <= 0f
            ? target
            : Mathf.LerpAngle(angleDeg, target, followSpeed * Time.deltaTime);

        ApplyRotation(angleDeg);
        if (haptics) activeController.HapticTick(dragHaptics, 0.015f);
        EmitValue(force: false);
    }

    float GetControllerAngle()
    {
        Vector3 world = activeController.transform.TransformDirection(referenceDir);
        Vector3 local = transform.parent.InverseTransformDirection(world);
        Vector3 projected = Vector3.ProjectOnPlane(local, Vector3.up);
        return Normalize180(Mathf.Atan2(projected.x, projected.z) * Mathf.Rad2Deg);
    }

    void ApplyRotation(float a)
        => transform.localRotation = Quaternion.AngleAxis(a, Vector3.up);

    void EmitValue(bool force)
    {
        float t = degreeRange <= 1e-6f
            ? Mathf.Repeat(angleDeg / 360f, 1f)
            : Mathf.InverseLerp(-degreeRange, degreeRange, angleDeg);
        value01 = t;
        if (force || Mathf.Abs(t - lastEmitted) > 0.0005f)
        {
            lastEmitted = t;
            onValueChanged01?.Invoke(t);
        }
    }

    static float Normalize180(float a)
    {
        a = (a + 180f) % 360f;
        if (a < 0f) a += 360f;
        return a - 180f;
    }
}

