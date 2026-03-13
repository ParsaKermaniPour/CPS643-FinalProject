using System.Collections;
using UnityEngine;

public class SafeDoorAutoOpen : MonoBehaviour
{
    public Transform door;
    public bool autoOpenOnStart = false;
    public float startDelay = 2f;
    public float openDuration = 1.2f;
    public float openAngle = 95f;
    public Vector3 localOpenAxis = Vector3.up;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine openRoutine;
    private bool isOpen;

    void Start()
    {
        CacheDoorRotations();

        if (autoOpenOnStart)
            OpenDoorWithDelay(startDelay);
    }

    public void OpenDoor()
    {
        OpenDoorWithDelay(0f);
    }

    public void OpenDoorWithDelay(float delay)
    {
        CacheDoorRotations();
        if (isOpen) return;

        if (openRoutine != null)
            StopCoroutine(openRoutine);

        openRoutine = StartCoroutine(OpenRoutine(Mathf.Max(0f, delay)));
    }

    public void ResetDoor()
    {
        CacheDoorRotations();

        if (openRoutine != null)
        {
            StopCoroutine(openRoutine);
            openRoutine = null;
        }

        door.localRotation = closedRotation;
        isOpen = false;
    }

    private void CacheDoorRotations()
    {
        if (door == null)
            door = transform;

        Vector3 axis = localOpenAxis.sqrMagnitude < 0.0001f ? Vector3.up : localOpenAxis.normalized;
        closedRotation = door.localRotation;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, axis);
    }

    private IEnumerator OpenRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float t = 0f;
        float duration = Mathf.Max(0.01f, openDuration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = p * p * (3f - 2f * p);
            door.localRotation = Quaternion.Slerp(closedRotation, openRotation, p);
            yield return null;
        }

        door.localRotation = openRotation;
        isOpen = true;
        openRoutine = null;
    }
}
