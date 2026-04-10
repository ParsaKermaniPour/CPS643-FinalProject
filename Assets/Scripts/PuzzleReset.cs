using UnityEngine;

public class PuzzleReset : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleResetEntry
    {
        [Tooltip("Prefab to respawn")]
        public GameObject prefab;

        [Tooltip("Current instance")]
        public GameObject liveInstance;

        [HideInInspector] public Vector3 cachedPosition;
        [HideInInspector] public Quaternion cachedRotation;
        [HideInInspector] public Transform cachedParent;
        [HideInInspector] public bool hasCached;
    }

    public enum ResetTiming
    {
        OnDeathTrigger,
        OnReturnButton,
        OnBoth
    }

    [Header("Puzzle Hard Reset")]
    [Tooltip("Enable puzzle reset")]
    public bool enablePuzzleHardReset = false;

    [Tooltip("When reset runs")]
    public ResetTiming resetTiming = ResetTiming.OnReturnButton;

    [Tooltip("Only these entries are reset")]
    public PuzzleResetEntry[] puzzleResetEntries;

    void Awake()
    {
        CachePuzzleEntryTransforms();
    }

    public void TryHardReset(ResetTiming trigger)
    {
        if (!enablePuzzleHardReset)
            return;

        bool shouldRun = resetTiming == ResetTiming.OnBoth || resetTiming == trigger;
        if (!shouldRun)
            return;

        HardResetConfiguredPuzzles();
    }

    public void HardResetConfiguredPuzzles()
    {
        if (puzzleResetEntries == null || puzzleResetEntries.Length == 0)
            return;

        for (int i = 0; i < puzzleResetEntries.Length; i++)
        {
            PuzzleResetEntry entry = puzzleResetEntries[i];
            if (entry == null)
                continue;

            if (entry.liveInstance != null)
            {
                if (!entry.hasCached)
                {
                    entry.cachedPosition = entry.liveInstance.transform.position;
                    entry.cachedRotation = entry.liveInstance.transform.rotation;
                    entry.cachedParent = entry.liveInstance.transform.parent;
                    entry.hasCached = true;
                }

                Destroy(entry.liveInstance);
                entry.liveInstance = null;
            }

            if (entry.prefab == null || !entry.hasCached)
                continue;

            GameObject fresh = Instantiate(entry.prefab, entry.cachedPosition, entry.cachedRotation, entry.cachedParent);
            entry.liveInstance = fresh;
        }
    }

    private void CachePuzzleEntryTransforms()
    {
        if (puzzleResetEntries == null)
            return;

        for (int i = 0; i < puzzleResetEntries.Length; i++)
        {
            PuzzleResetEntry entry = puzzleResetEntries[i];
            if (entry == null || entry.liveInstance == null)
                continue;

            entry.cachedPosition = entry.liveInstance.transform.position;
            entry.cachedRotation = entry.liveInstance.transform.rotation;
            entry.cachedParent = entry.liveInstance.transform.parent;
            entry.hasCached = true;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CachePuzzleEntryTransforms();
    }
#endif
}
