using UnityEngine;

/// <summary>
/// Camera swivel and tracking system.
/// - Attach this script to the cameraBase GameObject (the child that rotates on Y axis).
/// - In idle state, rotates around local Y axis back-and-forth between minAngle and maxAngle with pauses.
/// - When player is detected (via SecurityCameraSensor), abandons swivelinging and tracks player position.
/// - Tracking angle is clamped to [minAngle, maxAngle] so player can escape by moving outside range.
/// - All calculations in local space, accounting for any X/Z tilt of the base.
/// </summary>
public class CameraSwivel : MonoBehaviour
{
    private enum SwivelState { SwivelLeft, WaitLeft, SwivelRight, WaitRight }

    [Header("Detection")]
    [SerializeField] private SecurityCameraSensor cameraSensor;

    [Header("Swivel Settings")]
    [SerializeField] private float minAngle = -90f;
    [SerializeField] private float maxAngle = 90f;
    [SerializeField, Min(0.1f)] private float swivelSpeed = 30f;
    [SerializeField, Min(0.1f)] private float waitTimeAtEnd = 2f;

    private SwivelState currentSwivel;
    private bool isTracking;
    private float waitTimer;
    private float currentAngle;

    private void Start()
    {
        if (cameraSensor == null)
        {
            Debug.LogError("CameraSwivel: cameraSensor not assigned!");
            enabled = false;
            return;
        }

        // Clamp angles to -90 to 90 range
        minAngle = Mathf.Clamp(minAngle, -90f, 90f);
        maxAngle = Mathf.Clamp(maxAngle, -90f, 90f);

        // Ensure min is actually less than max
        if (minAngle > maxAngle)
        {
            float temp = minAngle;
            minAngle = maxAngle;
            maxAngle = temp;
        }

        // Initialize angle from current local Y rotation
        currentAngle = NormalizeAngle(transform.localEulerAngles.y);
        currentSwivel = SwivelState.SwivelRight;
        isTracking = false;
        waitTimer = 0f;
    }

    private void Update()
    {
        if (cameraSensor == null)
            return;

        // Check detection state transitions
        if (cameraSensor.IsDetected && !isTracking)
        {
            isTracking = true;
        }
        else if (!cameraSensor.IsDetected && isTracking)
        {
            isTracking = false;
        }

        // Update rotation based on current state
        if (isTracking)
        {
            UpdateTracking();
        }
        else
        {
            UpdateSwivel();
        }
    }

    private void UpdateSwivel()
    {
        // Update current angle from local Y rotation
        currentAngle = NormalizeAngle(transform.localEulerAngles.y);

        switch (currentSwivel)
        {
            case SwivelState.SwivelLeft:
                RotateCameraBaseTo(minAngle);
                if (Mathf.Abs(currentAngle - minAngle) < 1f)  // Close enough to target
                {
                    currentSwivel = SwivelState.WaitLeft;
                    waitTimer = waitTimeAtEnd;
                }
                break;

            case SwivelState.WaitLeft:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    currentSwivel = SwivelState.SwivelRight;
                }
                break;

            case SwivelState.SwivelRight:
                RotateCameraBaseTo(maxAngle);
                if (Mathf.Abs(currentAngle - maxAngle) < 1f)  // Close enough to target
                {
                    currentSwivel = SwivelState.WaitRight;
                    waitTimer = waitTimeAtEnd;
                }
                break;

            case SwivelState.WaitRight:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    currentSwivel = SwivelState.SwivelLeft;
                }
                break;
        }
    }

    private void UpdateTracking()
    {
        if (cameraSensor.playerCollider == null)
        {
            // Player not found, return to swivelinging
            isTracking = false;
            return;
        }

        Vector3 playerPos = cameraSensor.playerCollider.gameObject.transform.position;
        Vector3 dirToPlayer = (playerPos - transform.position).normalized;

        // Convert world-space direction to this object's local space
        // This is INDEPENDENT of parent rotation because we use transform.rotation (world rotation)
        Vector3 dirLocal = Quaternion.Inverse(transform.rotation) * dirToPlayer;

        // Calculate Y angle to face player in LOCAL space
        float targetAngle = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;

        // Clamp to swivel bounds BEFORE normalizing
        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);

        // Rotate toward target
        RotateCameraBaseTo(targetAngle);
    }

    private void RotateCameraBaseTo(float targetAngle)
    {
        // Update current angle
        currentAngle = NormalizeAngle(transform.localEulerAngles.y);

        // Calculate rotation direction (shortest path)
        float angleDiff = targetAngle - currentAngle;

        // Normalize difference to -180 to 180
        if (angleDiff > 180f)
            angleDiff -= 360f;
        else if (angleDiff < -180f)
            angleDiff += 360f;

        // Rotate toward target at swivelSpeed
        float rotationAmount = Mathf.Clamp(angleDiff, -swivelSpeed * Time.deltaTime, swivelSpeed * Time.deltaTime);
        float newAngle = currentAngle + rotationAmount;

        // Apply rotation
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.y = newAngle;
        transform.localEulerAngles = eulerAngles;
    }

    private float NormalizeAngle(float angle)
    {
        // Normalize to -180 to 180 range
        angle = angle % 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        return angle;
    }
}
