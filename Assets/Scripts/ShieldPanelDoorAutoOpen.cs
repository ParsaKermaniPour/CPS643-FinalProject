using System.Collections;
using UnityEngine;

public class ShieldPanelDoorAutoOpen : MonoBehaviour
{
    [Header("Door Setup")]
    public Transform door;
    public Transform hingePoint;
    public Vector3 hingeAxisLocal = Vector3.up;

    [Header("Timing")]
    public bool autoOpenOnStart = true;
    public float startDelay = 0.5f;
    public float openDuration = 1.2f;

    [Header("Motion")]
    public float openAngle = 95f;

    private Vector3 closedDoorPosition;
    private Quaternion closedDoorRotation;
    private Vector3 closedHingeWorldPosition;
    private Vector3 closedHingeWorldAxis;
    private Coroutine routine;
    private bool isOpen;

    void Start()
    {
        CacheClosedPose();

        if (autoOpenOnStart)
            OpenDoor();
    }

    public void OpenDoor()
    {
        if (door == null || hingePoint == null || isOpen)
            return;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(OpenRoutine());
    }

    public void ResetDoor()
    {
        if (door == null)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        door.position = closedDoorPosition;
        door.rotation = closedDoorRotation;
        isOpen = false;
    }

    private void CacheClosedPose()
    {
        if (door == null)
            door = transform;

        if (hingePoint == null)
            hingePoint = transform;

        closedDoorPosition = door.position;
        closedDoorRotation = door.rotation;
        closedHingeWorldPosition = hingePoint.position;

        Vector3 axis = hingeAxisLocal.sqrMagnitude < 0.0001f ? Vector3.up : hingeAxisLocal.normalized;
        closedHingeWorldAxis = hingePoint.TransformDirection(axis).normalized;
    }

    private IEnumerator OpenRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float duration = Mathf.Max(0.01f, openDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = p * p * (3f - 2f * p);

            ApplyPose(Mathf.Lerp(0f, openAngle, p));
            yield return null;
        }

        ApplyPose(openAngle);
        isOpen = true;
        routine = null;
    }

    private void ApplyPose(float angle)
    {
        Quaternion delta = Quaternion.AngleAxis(angle, closedHingeWorldAxis);

        door.position = closedHingeWorldPosition + delta * (closedDoorPosition - closedHingeWorldPosition);
        door.rotation = delta * closedDoorRotation;
    }
}
