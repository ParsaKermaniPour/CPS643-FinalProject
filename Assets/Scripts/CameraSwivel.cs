using UnityEngine;

/// <summary>
/// Camera swivel and tracking system.
/// - Attach this script to the cameraBase GameObject (rotates on X and Y axes).
/// - In idle state, rotates around local Y axis back-and-forth between minAngle and maxAngle with pauses.
/// - When player is detected (via SecurityCameraSensor), tracks player's head center with X and Y rotation.
/// - X rotation clamped to [-90, 90], Y rotation clamped to [minAngle, maxAngle].
/// - When player leaves, smoothly returns to idle swivel state.
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
    [SerializeField, Min(0.1f)] private float trackingSpeed = 60f;
    [SerializeField, Min(0.1f)] private float waitTimeAtEnd = 2f;
    [SerializeField, Min(0.1f)] private float returnToIdleSpeed = 45f;

    private SwivelState currentSwivel;
    private SwivelState previousSwivel;
    private bool isTracking;
    private bool isReturningToIdle;
    private float waitTimer;
    private float currentAngle;

    private float originalXRotation;  // X rotation when tracking starts
    private float targetIdleY;         // Y angle to return to

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

        // Store original X rotation (should remain constant during swivel)
        originalXRotation = NormalizeAngle(transform.localEulerAngles.x);

        // Initialize angle from current local Y rotation
        currentAngle = NormalizeAngle(transform.localEulerAngles.y);
        currentSwivel = SwivelState.SwivelRight;
        previousSwivel = SwivelState.SwivelRight;
        isTracking = false;
        isReturningToIdle = false;
        waitTimer = 0f;
    }

    private void Update()
    {
        if (cameraSensor == null)
            return;

        // Check detection state transitions
        if (cameraSensor.IsDetected && !isTracking && !isReturningToIdle)
        {
            previousSwivel = currentSwivel;  // Remember where we were
            isTracking = true;
        }
        else if (!cameraSensor.IsDetected && isTracking)
        {
            isTracking = false;
            isReturningToIdle = true;
            targetIdleY = (minAngle + maxAngle) * 0.5f;  // Return to center
        }

        // Update rotation based on current state
        if (isTracking)
        {
            UpdateTracking();
        }
        else if (isReturningToIdle)
        {
            UpdateReturnToIdle();
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
                RotateYOnly(minAngle, swivelSpeed);
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
                RotateYOnly(maxAngle, swivelSpeed);
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
            isReturningToIdle = true;
            targetIdleY = (minAngle + maxAngle) * 0.5f;
            return;
        }

        // Get player's head center
        Transform playerTransform = cameraSensor.playerCollider.gameObject.transform;
        Transform headCenter = playerTransform.parent.Find("HeadCenter");
        if (headCenter == null)
        {
            Debug.LogWarning("CameraSwivel: HeadCenter transform not found on player parent!");
            isTracking = false;
            isReturningToIdle = true;
            targetIdleY = (minAngle + maxAngle) * 0.5f;
            return;
        }

        Vector3 headPos = headCenter.position;
        Vector3 dirToHead = (headPos - transform.position).normalized;

        // Calculate X rotation (pitch) to face the head
        float targetXRotation = -Mathf.Asin(Mathf.Clamp(dirToHead.y, -1f, 1f)) * Mathf.Rad2Deg;
        targetXRotation = Mathf.Clamp(targetXRotation, -90f, 90f);

        // Calculate Y rotation (yaw) on horizontal plane
        Vector3 dirToHeadHorizontal = new Vector3(dirToHead.x, 0, dirToHead.z).normalized;
        Vector3 cameraForward = transform.rotation * Vector3.forward;
        Vector3 cameraForwardHorizontal = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
        
        float angleOffset = Vector3.SignedAngle(cameraForwardHorizontal, dirToHeadHorizontal, Vector3.up);
        float currentLocalY = NormalizeAngle(transform.localEulerAngles.y);
        float targetYRotation = currentLocalY + angleOffset;
        targetYRotation = Mathf.Clamp(targetYRotation, minAngle, maxAngle);

        // Apply rotations
        RotateXY(targetXRotation, targetYRotation, trackingSpeed);
    }

    private void UpdateReturnToIdle()
    {
        float currentX = NormalizeAngle(transform.localEulerAngles.x);
        float currentY = NormalizeAngle(transform.localEulerAngles.y);

        // Smoothly return X to original
        float xDiff = originalXRotation - currentX;
        if (Mathf.Abs(xDiff) > 180f)
            xDiff = xDiff > 0 ? xDiff - 360f : xDiff + 360f;

        float xAmount = Mathf.Clamp(xDiff, -returnToIdleSpeed * Time.deltaTime, returnToIdleSpeed * Time.deltaTime);
        float newX = currentX + xAmount;

        // Smoothly return Y to target idle position
        float yDiff = targetIdleY - currentY;
        if (Mathf.Abs(yDiff) > 180f)
            yDiff = yDiff > 0 ? yDiff - 360f : yDiff + 360f;

        float yAmount = Mathf.Clamp(yDiff, -returnToIdleSpeed * Time.deltaTime, returnToIdleSpeed * Time.deltaTime);
        float newY = currentY + yAmount;

        // Apply rotation
        ApplyXYRotation(newX, newY);

        // Check if returned to idle
        if (Mathf.Abs(xAmount) < 0.1f && Mathf.Abs(yAmount) < 0.1f)
        {
            isReturningToIdle = false;
            // Resume swivel from center
            currentSwivel = currentAngle > 0 ? SwivelState.SwivelLeft : SwivelState.SwivelRight;
        }
    }

    private void RotateYOnly(float targetAngle, float rotationSpeed)
    {
        // Rotate only Y, keeping X at original
        currentAngle = NormalizeAngle(transform.localEulerAngles.y);

        float angleDiff = targetAngle - currentAngle;
        if (angleDiff > 180f)
            angleDiff -= 360f;
        else if (angleDiff < -180f)
            angleDiff += 360f;

        float rotationAmount = Mathf.Clamp(angleDiff, -rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime);
        float newAngle = currentAngle + rotationAmount;

        ApplyXYRotation(originalXRotation, newAngle);
    }

    private void RotateXY(float targetX, float targetY, float rotationSpeed)
    {
        // Rotate both X and Y toward targets
        float currentX = NormalizeAngle(transform.localEulerAngles.x);
        float currentY = NormalizeAngle(transform.localEulerAngles.y);

        // X rotation
        float xDiff = targetX - currentX;
        if (Mathf.Abs(xDiff) > 180f)
            xDiff = xDiff > 0 ? xDiff - 360f : xDiff + 360f;
        float xAmount = Mathf.Clamp(xDiff, -rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime);
        float newX = currentX + xAmount;

        // Y rotation
        float yDiff = targetY - currentY;
        if (Mathf.Abs(yDiff) > 180f)
            yDiff = yDiff > 0 ? yDiff - 360f : yDiff + 360f;
        float yAmount = Mathf.Clamp(yDiff, -rotationSpeed * Time.deltaTime, rotationSpeed * Time.deltaTime);
        float newY = currentY + yAmount;

        ApplyXYRotation(newX, newY);
    }

    private void ApplyXYRotation(float x, float y)
    {
        Vector3 eulerAngles = transform.localEulerAngles;
        eulerAngles.x = x;
        eulerAngles.y = y;
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
