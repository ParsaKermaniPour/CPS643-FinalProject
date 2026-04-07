using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ExitPortalTeleport : MonoBehaviour
{
    [Header("Player Detection")]
    [Tooltip("Player tag used when no OVRCameraRig is found on the entering collider hierarchy")]
    public string playerTag = "Player";

    [Tooltip("Treat any collider under an OVRCameraRig as the player")]
    public bool detectOVRCameraRig = true;

    [Header("Objective Requirement")]
    [Tooltip("Allowed held item tag #1")]
    public string requiredTagA = "Ray_Gun";

    [Tooltip("Allowed held item tag #2")]
    public string requiredTagB = "Potion";

    [Tooltip("If true, both tags must be held at once. Otherwise either one is enough")]
    public bool requireBothTags = false;

    [Tooltip("Allow teleport when the collider entering this trigger is a required held objective item")]
    public bool allowHeldObjectiveTriggerEntry = true;

    [Header("Teleport Target")]
    [Tooltip("Optional transform target. If empty, uses fallback position below")]
    public Transform teleportTarget;

    [Tooltip("Fallback world position if no teleport target transform is assigned")]
    public Vector3 fallbackTeleportPosition = new Vector3(55f, 0f, -60f);

    [Tooltip("Copy target rotation when teleporting")]
    public bool applyTargetRotation = true;

    [Header("Teleport Safety")]
    [Tooltip("Snap destination to the nearest floor below target using a raycast")]
    public bool snapToGround = true;

    [Tooltip("Layers treated as floor for teleport snap")]
    public LayerMask groundLayers = ~0;

    [Tooltip("How high above target to start floor raycast")]
    public float groundRayStartHeight = 5f;

    [Tooltip("How far downward to search for floor")]
    public float groundRayDistance = 30f;

    [Tooltip("Lift applied above hit floor point")]
    public float groundOffset = 0.05f;

    [Tooltip("Temporarily disable CharacterController while repositioning")]
    public bool disableCharacterControllerDuringTeleport = true;

    [Header("Celebration")]
    public ParticleSystem successParticles;
    public AudioSource successAudioSource;
    public AudioClip successClip;

    [Tooltip("Invoked after a successful objective check and teleport")]
    public UnityEvent onSuccess;

    [Tooltip("Invoked when player enters without required held objective")]
    public UnityEvent onFail;

    [Header("Safety")]
    [Tooltip("Prevents rapid retrigger spam")]
    public float triggerCooldownSeconds = 1f;

    private float nextAllowedTriggerTime;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedTriggerTime)
            return;

        bool hasPlayer = TryGetPlayerRoot(other, out Transform playerRoot, out OVRCameraRig rig);

        // VR fallback: many rigs don't have a body collider, but held item colliders do enter triggers.
        if (!hasPlayer && allowHeldObjectiveTriggerEntry)
        {
            if (TryGetHeldObjectiveFromCollider(other, out string hitTag))
            {
                rig = FindFirstObjectByType<OVRCameraRig>();
                if (rig != null)
                {
                    playerRoot = rig.transform;
                    hasPlayer = true;
                }
            }
        }

        if (!hasPlayer)
            return;

        nextAllowedTriggerTime = Time.time + Mathf.Max(0f, triggerCooldownSeconds);

        if (!HasRequiredHeldObjective())
        {
            onFail?.Invoke();
            return;
        }

        TeleportPlayer(playerRoot, rig);
        TriggerCelebration();
        onSuccess?.Invoke();
    }

    private bool TryGetPlayerRoot(Collider other, out Transform playerRoot, out OVRCameraRig rig)
    {
        playerRoot = null;
        rig = null;

        if (other == null)
            return false;

        if (detectOVRCameraRig)
        {
            rig = other.GetComponentInParent<OVRCameraRig>();
            if (rig != null)
            {
                playerRoot = rig.transform;
                return true;
            }
        }

        if (other.CompareTag(playerTag))
        {
            playerRoot = other.transform.root;
            return true;
        }

        Transform root = other.transform.root;
        if (root != null && root.CompareTag(playerTag))
        {
            playerRoot = root;
            return true;
        }

        return false;
    }

    private bool HasRequiredHeldObjective()
    {
        bool hasA = IsTaggedItemHeld(requiredTagA);
        bool hasB = IsTaggedItemHeld(requiredTagB);

        return requireBothTags ? hasA && hasB : hasA || hasB;
    }

    private bool IsTaggedItemHeld(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            return false;

        GameObject[] taggedItems;
        try
        {
            taggedItems = GameObject.FindGameObjectsWithTag(tagName);
        }
        catch
        {
            return false;
        }

        for (int i = 0; i < taggedItems.Length; i++)
        {
            GameObject item = taggedItems[i];
            if (item == null)
                continue;

            VRGrabbableBase grabbable = item.GetComponent<VRGrabbableBase>();
            if (grabbable == null)
                grabbable = item.GetComponentInParent<VRGrabbableBase>();
            if (grabbable == null)
                grabbable = item.GetComponentInChildren<VRGrabbableBase>();

            if (grabbable != null && grabbable.IsGrabbed)
                return true;
        }

        return false;
    }

    private bool TryGetHeldObjectiveFromCollider(Collider other, out string hitTag)
    {
        hitTag = string.Empty;
        if (other == null)
            return false;

        Transform t = other.transform;
        string[] needed = requireBothTags
            ? new string[] { requiredTagA, requiredTagB }
            : new string[] { requiredTagA, requiredTagB };

        for (int i = 0; i < needed.Length; i++)
        {
            string tagName = needed[i];
            if (string.IsNullOrWhiteSpace(tagName))
                continue;

            bool matches = (t.CompareTag(tagName))
                || (t.root != null && t.root.CompareTag(tagName))
                || (t.parent != null && t.parent.CompareTag(tagName));

            if (!matches)
                continue;

            VRGrabbableBase grabbable = t.GetComponent<VRGrabbableBase>();
            if (grabbable == null)
                grabbable = t.GetComponentInParent<VRGrabbableBase>();
            if (grabbable == null)
                grabbable = t.GetComponentInChildren<VRGrabbableBase>();

            if (grabbable != null && grabbable.IsGrabbed)
            {
                hitTag = tagName;
                return true;
            }
        }

        return false;
    }

    private void TeleportPlayer(Transform playerRoot, OVRCameraRig rig)
    {
        Vector3 targetPosition = teleportTarget != null ? teleportTarget.position : fallbackTeleportPosition;
        Quaternion targetRotation = teleportTarget != null ? teleportTarget.rotation : playerRoot.rotation;

        if (snapToGround)
            targetPosition = ResolveGroundedTarget(targetPosition);

        if (rig != null && rig.centerEyeAnchor != null)
        {
            Vector3 eyeToRigOffset = rig.transform.position - rig.centerEyeAnchor.position;
            Vector3 rigTarget = targetPosition + eyeToRigOffset;
            ApplyTransformSafely(rig.transform, rigTarget, targetRotation, applyTargetRotation && teleportTarget != null);

            return;
        }

        ApplyTransformSafely(playerRoot, targetPosition, targetRotation, applyTargetRotation && teleportTarget != null);
    }

    private Vector3 ResolveGroundedTarget(Vector3 rawTarget)
    {
        Vector3 rayOrigin = rawTarget + Vector3.up * Mathf.Max(0.1f, groundRayStartHeight);
        float rayDistance = Mathf.Max(0.5f, groundRayDistance);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundLayers, QueryTriggerInteraction.Ignore))
            return hit.point + Vector3.up * Mathf.Max(0f, groundOffset);

        return rawTarget;
    }

    private void ApplyTransformSafely(Transform target, Vector3 position, Quaternion rotation, bool applyRotation)
    {
        if (target == null)
            return;

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc == null)
            cc = target.GetComponentInParent<CharacterController>();

        bool restored = false;
        if (disableCharacterControllerDuringTeleport && cc != null && cc.enabled)
        {
            cc.enabled = false;
            restored = true;
        }

        target.position = position;
        if (applyRotation)
            target.rotation = rotation;

        if (restored)
            cc.enabled = true;
    }

    private void TriggerCelebration()
    {
        if (successParticles != null)
            successParticles.Play();

        if (successAudioSource != null && successClip != null)
            successAudioSource.PlayOneShot(successClip);
    }
}
