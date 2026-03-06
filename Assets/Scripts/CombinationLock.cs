using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Combination lock logic — listens to DialInteractable events and enforces:
///   Step 1: Spin CLOCKWISE     → land on number 1
///   Step 2: Spin COUNTER-CW   → land on number 2
///   Step 3: Spin CLOCKWISE     → land on number 3
///
/// Attach this to the Dial GameObject.
/// Wire up DialInteractable's onNumberChanged → CombinationLock.OnNumberChanged
///         DialInteractable's onDeltaChanged  → CombinationLock.OnDeltaChanged
/// </summary>
public class CombinationLock : MonoBehaviour
{
    [Header("Combination")]
    [Tooltip("First number — must be reached spinning CLOCKWISE")]
    public int number1 = 30;

    [Tooltip("Second number — must be reached spinning COUNTER-CLOCKWISE")]
    public int number2 = 65;

    [Tooltip("Third number — must be reached spinning CLOCKWISE")]
    public int number3 = 10;

    [Tooltip("How many ticks of tolerance (e.g. 2 means ±2 numbers accepted)")]
    public int tolerance = 2;

    [Header("Visual Indicator")]
    [Tooltip("Drag your UnlockIndicator GameObject here (the spinning cube)")]
    public UnlockIndicator unlockIndicator;

    [Header("Events")]
    public UnityEvent onUnlocked;
    public UnityEvent onStepCompleted;
    public UnityEvent onReset;

    // 0 = waiting for step 1, 1 = waiting for step 2, 2 = waiting for step 3, 3 = unlocked
    public int CurrentStep { get; private set; } = 0;

    private float currentDelta = 0f;    // last rotation delta (positive = CW, negative = CCW)
    private bool stepJustAdvanced = false;

    // ---- Called from DialInteractable.onDeltaChanged ----
    public void OnDeltaChanged(float delta)
    {
        currentDelta = delta;
    }

    // ---- Called from DialInteractable.onNumberChanged ----
    public void OnNumberChanged(int number)
    {
        if (CurrentStep == 3) return; // already unlocked

        stepJustAdvanced = false;

        switch (CurrentStep)
        {
            case 0: // Step 1: must be spinning CLOCKWISE (positive delta)
                if (IsClockwise() && IsWithin(number, number1))
                    AdvanceStep();
                break;

            case 1: // Step 2: must be spinning COUNTER-CLOCKWISE (negative delta)
                if (IsCounterClockwise() && IsWithin(number, number2))
                    AdvanceStep();
                break;

            case 2: // Step 3: must be spinning CLOCKWISE (positive delta)
                if (IsClockwise() && IsWithin(number, number3))
                    AdvanceStep();
                break;
        }
    }

    private void AdvanceStep()
    {
        CurrentStep++;
        stepJustAdvanced = true;

        Debug.Log($"[CombinationLock] Step {CurrentStep} complete!");

        if (CurrentStep == 3)
        {
            Debug.Log("[CombinationLock] UNLOCKED!");
            onUnlocked?.Invoke();

            if (unlockIndicator != null)
                unlockIndicator.TriggerUnlock();
        }
        else
        {
            onStepCompleted?.Invoke();

            if (unlockIndicator != null)
                unlockIndicator.ShowStepProgress(CurrentStep);
        }
    }

    /// <summary>Reset the lock back to step 1 (e.g. when player spins wrong direction)</summary>
    public void ResetLock()
    {
        CurrentStep = 0;
        Debug.Log("[CombinationLock] Reset.");
        onReset?.Invoke();

        if (unlockIndicator != null)
            unlockIndicator.Reset();
    }

    private bool IsClockwise()        => currentDelta > 0.5f;
    private bool IsCounterClockwise() => currentDelta < -0.5f;

    private bool IsWithin(int number, int target)
    {
        // Handle wrap-around (e.g. tolerance around 0/100)
        int diff = Mathf.Abs(number - target);
        return diff <= tolerance || diff >= (100 - tolerance);
    }
}
