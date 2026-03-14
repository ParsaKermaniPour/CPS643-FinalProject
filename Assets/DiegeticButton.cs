using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class DiegeticButton : Interactable
{
    public enum LocalAxis { X, Y, Z }

    [Header("Button Motion Direction")]
    public LocalAxis pressAxis = LocalAxis.Y;
    public float maxPressDepth = 0.02f;
    public float deadZone = 0.003f;

    [Range(0.1f, 0.95f)]
    public float pressThreshold = 0.7f;
    public float springSpeed = 25f;
    public float cooldown = 0.15f;

    [Header("Haptics")]
    public bool haptics = true;
    public float armedTickAmplitude = 0.15f;
    public float armedTickDuration = 0.015f;
    public float clickAmplitude = 0.40f;
    public float clickDuration = 0.035f;

    [Header("Events")]
    public UnityEvent onPressed;

    Vector3 restPosition;
    OVRController activeController;
    float depth;
    bool isArmed;
    bool hapticFired;
    float cooldownUntil;

    void Awake()
    {
        restPosition = transform.localPosition;
        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void Update()
    {
        if (activeController != null)
        {
            Vector3 ctrlLocal = transform.parent.InverseTransformPoint(activeController.transform.position);
            float raw = Vector3.Dot(-PressAxis(), ctrlLocal - restPosition);
            depth = Mathf.Clamp(raw - deadZone, 0f, maxPressDepth);

            if (!isArmed && Time.time > cooldownUntil && depth >= maxPressDepth * pressThreshold)
            {
                isArmed = true;
                if (haptics && !hapticFired)
                {
                    activeController.HapticTick(armedTickAmplitude, armedTickDuration);
                    hapticFired = true;
                }
            }
        }
        else
        {
            depth = Mathf.MoveTowards(depth, 0f, springSpeed * Time.deltaTime);
            if (depth <= 0.005f)
            {
                isArmed = false;
                hapticFired = false;
            }
        }

        transform.localPosition = restPosition - depth * PressAxis();
    }

    public override void OnTouchEnter(OVRController ctrl)
    {
        activeController = ctrl;
        isArmed = false;
        hapticFired = false;
    }

    public override void OnTouchExit(OVRController ctrl)
    {
        if (ctrl != activeController) return;

        if (isArmed && Time.time >= cooldownUntil)
        {
            onPressed?.Invoke();
            cooldownUntil = Time.time + cooldown;
            if (haptics) activeController.HapticTick(clickAmplitude, clickDuration);
        }

        activeController = null;
        isArmed = false;
        hapticFired = false;
    }

    Vector3 PressAxis()
        => pressAxis == LocalAxis.X ? Vector3.right
         : pressAxis == LocalAxis.Y ? Vector3.up
         : Vector3.forward;
}

