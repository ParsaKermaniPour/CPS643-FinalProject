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

    [Header("Events")]
    public UnityEvent onUnlocked;
    public UnityEvent onStepCompleted;

    public int CurrentStep { get; private set; } = 0;

    private DialInteractable dial;

    void Start()
    {
        dial = GetComponent<DialInteractable>();
        if (dial != null)
            dial.onNumberChanged.AddListener(OnNumberChanged);
        else
            Debug.LogError("[CombinationLock] No DialInteractable found on this GameObject!");
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
        CurrentStep++;
        Debug.Log($"[CombinationLock] Step {CurrentStep} done! Indicator assigned: {unlockIndicator != null}");

        if (CurrentStep == 3)
        {
            Debug.Log("[CombinationLock] UNLOCKED!");
            onUnlocked?.Invoke();
            if (unlockIndicator != null) unlockIndicator.TriggerUnlock();
            else Debug.LogError("[CombinationLock] UnlockIndicator is NULL — drag the cube into the Unlock Indicator field on this component!");
        }
        else
        {
            onStepCompleted?.Invoke();
            if (unlockIndicator != null) unlockIndicator.ShowStepProgress(CurrentStep);
            else Debug.LogError("[CombinationLock] UnlockIndicator is NULL — drag the cube into the Unlock Indicator field on this component!");
        }
    }

    public void ResetLock()
    {
        CurrentStep = 0;
        Debug.Log("[CombinationLock] Reset.");
        if (unlockIndicator != null) unlockIndicator.Reset();
    }
}
