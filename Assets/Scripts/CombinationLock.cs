using UnityEngine;
using UnityEngine.Events;

public class CombinationLock : MonoBehaviour
{
    [Header("Combination")]
    public int number1 = 30;
    public int number2 = 65;
    public int number3 = 10;

    [Header("Visual Indicator")]
    public UnlockIndicator unlockIndicator;

    [Header("Safe Door")]
    public SafeDoorAutoOpen safeDoor;

    [Header("Events")]
    public UnityEvent onUnlocked;
    public UnityEvent onStepCompleted;

    public int CurrentStep { get; private set; } = 0;

    private DialInteractable dial;
    private bool hasLoggedMissingIndicator;

    void Start()
    {
        dial = GetComponent<DialInteractable>();
        if (dial != null)
            dial.onNumberChanged.AddListener(OnNumberChanged);
        else
            Debug.LogError("[CombinationLock] No DialInteractable found on this GameObject!");

        if (safeDoor == null)
            safeDoor = FindFirstObjectByType<SafeDoorAutoOpen>();

        ResolveMissingReferences();
    }

    void OnDestroy()
    {
        if (dial != null)
            dial.onNumberChanged.RemoveListener(OnNumberChanged);
    }

    private void OnNumberChanged(int number)
    {
        if (CurrentStep == 3) return;

        int target = CurrentStep == 0 ? number1 :
                     CurrentStep == 1 ? number2 : number3;

        Debug.Log($"[Lock] Step {CurrentStep + 1}/3 | Dial: {number} | Target: {target}");

        if (number == target)
            AdvanceStep();
    }

    private void AdvanceStep()
    {
        ResolveMissingReferences();

        CurrentStep++;
        Debug.Log($"[CombinationLock] Step {CurrentStep} done! Indicator assigned: {unlockIndicator != null}");

        if (CurrentStep == 3)
        {
            Debug.Log("[CombinationLock] UNLOCKED!");
            onUnlocked?.Invoke();
            if (safeDoor != null) safeDoor.OpenDoor();
            else Debug.LogError("[CombinationLock] SafeDoorAutoOpen is NULL — assign the safe door script in this component or add one in scene.");
            if (unlockIndicator != null) unlockIndicator.TriggerUnlock();
            else LogMissingIndicatorOnce();
        }
        else
        {
            onStepCompleted?.Invoke();
            if (unlockIndicator != null) unlockIndicator.ShowStepProgress(CurrentStep);
            else LogMissingIndicatorOnce();
        }
    }

    public void ResetLock()
    {
        CurrentStep = 0;
        Debug.Log("[CombinationLock] Reset.");
        if (unlockIndicator != null) unlockIndicator.Reset();
    }

    private void ResolveMissingReferences()
    {
        if (unlockIndicator == null)
        {
            unlockIndicator = GetComponentInChildren<UnlockIndicator>(true);

            if (unlockIndicator == null)
                unlockIndicator = FindFirstObjectByType<UnlockIndicator>();
        }

        if (safeDoor == null)
            safeDoor = FindFirstObjectByType<SafeDoorAutoOpen>();
    }

    private void LogMissingIndicatorOnce()
    {
        if (hasLoggedMissingIndicator)
            return;

        hasLoggedMissingIndicator = true;
        Debug.LogWarning("[CombinationLock] UnlockIndicator was not assigned and no indicator was found in scene. Assign the cube in the Unlock Indicator field or add an UnlockIndicator component to a scene object.");
    }
}
