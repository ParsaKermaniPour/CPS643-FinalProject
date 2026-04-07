using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CombinationLock : MonoBehaviour
{
    private enum TurnDirection
    {
        None,
        Clockwise,
        CounterClockwise
    }

    [Header("Combination")]
    public int number1 = 30;
    public int number2 = 65;
    public int number3 = 10;

    [Header("Turn Rules")]
    [Tooltip("If enabled: step 1 requires clockwise, step 2 requires counterclockwise, step 3 requires clockwise")]
    public bool requireStepDirections = true;

    [Tooltip("Ignore tiny delta noise from hand jitter")]
    public float directionDeadzone = 0.01f;

    [Tooltip("Toggle this if your dial's perceived clockwise/counterclockwise feels reversed")]
    public bool invertDirection = false;

    [Tooltip("If true, any failed step attempt resets progress back to step 1")]
    public bool resetToStepOneOnFail = true;

    [Header("Speed Rules")]
    [Tooltip("If enabled, the target only registers when averaged dial speed is below the threshold")]
    public bool requireSlowAverageSpeed = true;

    [Tooltip("Maximum allowed averaged speed in degrees/second when landing on a target. Lower is harder")]
    public float maxAverageSpeedDegPerSecond = 260f;

    [Tooltip("Smoothing window in seconds for average speed (higher = smoother, fewer spike fails)")]
    public float speedAverageWindowSeconds = 0.12f;

    [Header("Audio")]
    [Tooltip("Audio source used for safe lock feedback. If missing, one is created on this GameObject.")]
    public AudioSource sfxSource;

    [Tooltip("Played when a step is successfully registered (step 1 and 2).")]
    public AudioClip registerClip;

    [Tooltip("Played on failed attempt (wrong direction or too fast).")]
    public AudioClip failClip;

    [Tooltip("Played when the final step unlocks the safe.")]
    public AudioClip finalUnlockClip;

    [Range(0f, 1f)]
    public float sfxVolume = 0.9f;

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
    private TurnDirection lastTurnDirection = TurnDirection.None;
    private float averagedAngularSpeedDegPerSecond;

    void Start()
    {
        dial = GetComponent<DialInteractable>();
        if (dial != null)
        {
            dial.onNumberChanged.AddListener(OnNumberChanged);
            dial.onDeltaChanged.AddListener(OnDeltaChanged);
        }
        else
            Debug.LogError("[CombinationLock] No DialInteractable found on this GameObject!");

        if (safeDoor == null)
            safeDoor = FindFirstObjectByType<SafeDoorAutoOpen>();

        ResolveMissingReferences();
        EnsureSfxSource();
    }

    void OnDestroy()
    {
        if (dial != null)
        {
            dial.onNumberChanged.RemoveListener(OnNumberChanged);
            dial.onDeltaChanged.RemoveListener(OnDeltaChanged);
        }
    }

    public void OnDeltaChanged(float delta)
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float instantSpeed = Mathf.Abs(delta) / dt;
        float window = Mathf.Max(0.01f, speedAverageWindowSeconds);
        float alpha = 1f - Mathf.Exp(-dt / window);
        averagedAngularSpeedDegPerSecond = Mathf.Lerp(averagedAngularSpeedDegPerSecond, instantSpeed, alpha);

        if (Mathf.Abs(delta) < directionDeadzone)
            return;

        bool clockwisePositive = !invertDirection;
        bool isClockwise = clockwisePositive ? delta > 0f : delta < 0f;
        lastTurnDirection = isClockwise ? TurnDirection.Clockwise : TurnDirection.CounterClockwise;
    }

    public void OnNumberChanged(int number)
    {
        if (CurrentStep == 3) return;

        int target = CurrentStep == 0 ? number1 :
                     CurrentStep == 1 ? number2 : number3;

        Debug.Log($"[Lock] Step {CurrentStep + 1}/3 | Dial: {number} | Target: {target} | Dir: {lastTurnDirection} | AvgSpeed: {averagedAngularSpeedDegPerSecond:F1}");

        if (number != target)
            return;

        if (requireStepDirections && !MatchesRequiredDirectionForStep())
        {
            HandleFailedAttempt($"[CombinationLock] Failed step {CurrentStep + 1}: wrong direction on target {target}. Resetting to step 1.");
            return;
        }

        if (requireSlowAverageSpeed && averagedAngularSpeedDegPerSecond > maxAverageSpeedDegPerSecond)
        {
            HandleFailedAttempt($"[CombinationLock] Failed step {CurrentStep + 1}: average speed {averagedAngularSpeedDegPerSecond:F1} deg/s exceeded limit {maxAverageSpeedDegPerSecond:F1} deg/s on target {target}. Resetting to step 1.");
            return;
        }

        AdvanceStep();
    }

    private bool MatchesRequiredDirectionForStep()
    {
        TurnDirection required = CurrentStep == 1
            ? TurnDirection.CounterClockwise
            : TurnDirection.Clockwise;

        return lastTurnDirection == required;
    }

    private void AdvanceStep()
    {
        ResolveMissingReferences();

        CurrentStep++;
        Debug.Log($"[CombinationLock] Step {CurrentStep} done! Indicator assigned: {unlockIndicator != null}");

        if (CurrentStep == 3)
            PlaySfx(finalUnlockClip);
        else
            PlaySfx(registerClip);

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
        lastTurnDirection = TurnDirection.None;
        averagedAngularSpeedDegPerSecond = 0f;
        Debug.Log("[CombinationLock] Reset.");
        if (unlockIndicator != null) unlockIndicator.Reset();
    }

    private void HandleFailedAttempt(string reason)
    {
        Debug.LogWarning(reason);
        PlaySfx(failClip);
        if (resetToStepOneOnFail)
            ResetLock();
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
            return;

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Logarithmic;
            sfxSource.minDistance = 0.2f;
            sfxSource.maxDistance = 8f;
        }
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSfxSource();
        if (sfxSource == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoAssignAudioClipsInEditor();
    }

    private void AutoAssignAudioClipsInEditor()
    {
        if (registerClip == null)
            registerClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/register.mp3");

        if (failClip == null)
            failClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/glass.mp3");

        if (finalUnlockClip == null)
            finalUnlockClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/final-unlock.mp3");

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();
    }
#endif

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
