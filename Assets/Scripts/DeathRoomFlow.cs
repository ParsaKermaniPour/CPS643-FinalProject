using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeathRoomFlow : MonoBehaviour
{
    [System.Serializable]
    public class PuzzleResetEntry
    {
        [Tooltip("Prefab asset to recreate")]
        public GameObject prefab;

        [Tooltip("Current scene instance to replace")]
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

    public enum FlowMode
    {
        DeathTrigger,
        ReturnButton
    }

    [Header("Mode")]
    public FlowMode mode = FlowMode.DeathTrigger;

    [Header("Detection")]
    [Tooltip("For DeathTrigger mode")]
    public string playerTag = "Player";

    [Tooltip("For ReturnButton mode")]
    public string buttonActivationTag = "Fingertip";

    public bool detectOVRCameraRig = true;

    [Header("Targets")]
    public Transform deathRoomSpawn;
    public Transform planningRoomSpawn;

    public Vector3 fallbackDeathRoomPosition = new Vector3(0f, 2f, 0f);
    public Vector3 fallbackPlanningRoomPosition = new Vector3(55f, 0f, -60f);

    [Header("Teleport")]
    public bool applyTargetRotation = true;
    public bool disableCharacterControllerDuringTeleport = true;

    [Header("Teleport Safety")]
    [Tooltip("Snap destination to floor under the spawn point")]
    public bool snapToGround = true;

    [Tooltip("Layers considered valid ground")]
    public LayerMask groundLayers = ~0;

    [Tooltip("Raycast start height above spawn")]
    public float groundRayStartHeight = 4f;

    [Tooltip("How far down to search for floor")]
    public float groundRayDistance = 20f;

    [Tooltip("Final lift above ground hit point")]
    public float groundOffset = 0.05f;

    [Header("Red Glow (Optional)")]
    [Tooltip("Enable to pulse red emissive glow on assigned renderers")]
    public bool pulseRedGlow = false;

    public Renderer[] redGlowRenderers;
    public Color redGlowColor = new Color(1f, 0.1f, 0.1f, 1f);
    public float minGlow = 0.4f;
    public float maxGlow = 2.2f;
    public float glowPulseSpeed = 2f;

    [Header("Safety")]
    public float cooldownSeconds = 0.75f;

    [Header("Puzzle Hard Reset")]
    [Tooltip("Enable hard reset by destroying and re-instantiating configured puzzle prefabs")]
    public bool enablePuzzleHardReset = false;

    [Tooltip("When to run the hard reset")]
    public ResetTiming resetTiming = ResetTiming.OnReturnButton;

    [Tooltip("Only these entries are reset. Nothing else in scene is touched.")]
    public PuzzleResetEntry[] puzzleResetEntries;

    private float nextAllowedTime;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    void Awake()
    {
        CachePuzzleEntryTransforms();
    }

    void Update()
    {
        if (!pulseRedGlow || redGlowRenderers == null || redGlowRenderers.Length == 0)
            return;

        float t = (Mathf.Sin(Time.time * Mathf.Max(0.01f, glowPulseSpeed)) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minGlow, maxGlow, t);
        Color emissive = redGlowColor * intensity;

        for (int i = 0; i < redGlowRenderers.Length; i++)
        {
            Renderer r = redGlowRenderers[i];
            if (r == null) continue;

            Material m = r.material;
            if (m == null) continue;

            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emissive);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedTime)
            return;

        if (!IsValidActivator(other))
            return;

        nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);

        if (mode == FlowMode.DeathTrigger)
        {
            TryHardReset(ResetTiming.OnDeathTrigger);
            TeleportRigTo(deathRoomSpawn, fallbackDeathRoomPosition);
        }
        else
        {
            TryHardReset(ResetTiming.OnReturnButton);
            TeleportRigTo(planningRoomSpawn, fallbackPlanningRoomPosition);
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

    private void TryHardReset(ResetTiming trigger)
    {
        if (!enablePuzzleHardReset)
            return;

        bool shouldRun = resetTiming == ResetTiming.OnBoth || resetTiming == trigger;
        if (!shouldRun)
            return;

        HardResetConfiguredPuzzles();
    }

    private void HardResetConfiguredPuzzles()
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

    private bool IsValidActivator(Collider other)
    {
        if (other == null)
            return false;

        if (mode == FlowMode.ReturnButton)
            return other.CompareTag(buttonActivationTag);

        if (detectOVRCameraRig && other.GetComponentInParent<OVRCameraRig>() != null)
            return true;

        if (other.CompareTag(playerTag))
            return true;

        Transform root = other.transform.root;
        return root != null && root.CompareTag(playerTag);
    }

    private void TeleportRigTo(Transform targetTransform, Vector3 fallbackPosition)
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig == null)
            return;

        Vector3 targetPos = targetTransform != null ? targetTransform.position : fallbackPosition;
        Quaternion targetRot = targetTransform != null ? targetTransform.rotation : rig.transform.rotation;

        if (snapToGround)
            targetPos = ResolveGroundedTarget(targetPos);

        CharacterController cc = rig.GetComponent<CharacterController>();
        if (cc == null)
            cc = rig.GetComponentInParent<CharacterController>();

        bool restoreController = false;
        if (disableCharacterControllerDuringTeleport && cc != null && cc.enabled)
        {
            cc.enabled = false;
            restoreController = true;
        }

        if (rig.centerEyeAnchor != null)
        {
            Vector3 eyeToRigOffset = rig.transform.position - rig.centerEyeAnchor.position;
            // Keep only horizontal offset so we don't sink/float from head-height differences.
            eyeToRigOffset.y = 0f;
            targetPos += eyeToRigOffset;
        }

        rig.transform.position = targetPos;
        if (applyTargetRotation && targetTransform != null)
            rig.transform.rotation = targetRot;

        if (restoreController)
            cc.enabled = true;
    }

    private Vector3 ResolveGroundedTarget(Vector3 rawTarget)
    {
        Vector3 rayOrigin = rawTarget + Vector3.up * Mathf.Max(0.1f, groundRayStartHeight);
        float rayDistance = Mathf.Max(0.5f, groundRayDistance);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * Mathf.Max(0f, groundOffset);

        return rawTarget;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CachePuzzleEntryTransforms();
    }
#endif
}
