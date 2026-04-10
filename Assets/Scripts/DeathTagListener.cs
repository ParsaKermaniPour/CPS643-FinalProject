using UnityEngine;

public class DeathTagListener : MonoBehaviour
{
    [Header("Detection")]
    public string deathTag = "Death";

    [Tooltip("Extra reliability: check for overlap with Death-tagged triggers every frame")]
    public bool useOverlapCheck = true;

    [Tooltip("Radius for overlap check around player head/body")]
    public float overlapCheckRadius = 0.2f;

    [Tooltip("Approximate body height used for capsule overlap")]
    public float overlapBodyHeight = 1.6f;

    [Tooltip("Extra downward check distance from rig root for death floors")]
    public float feetRayDistance = 0.35f;

    [Header("Teleport")]
    public Transform deathRoomSpawn;
    public Vector3 fallbackDeathRoomPosition = new Vector3(0f, 2f, 0f);
    public bool applyTargetRotation = true;
    public bool disableCharacterControllerDuringTeleport = true;

    [Header("Safety")]
    public float cooldownSeconds = 0.75f;

    private float nextAllowedTime;
    private readonly Collider[] overlapHits = new Collider[32];

    private void Update()
    {
        if (!useOverlapCheck || Time.time < nextAllowedTime)
            return;

        OVRCameraRig rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null)
            rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig == null)
            return;

        if (IsTouchingDeath(rig))
        {
            nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
            TeleportToDeathRoom();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedTime)
            return;

        if (other == null || !other.CompareTag(deathTag))
            return;

        nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
        TeleportToDeathRoom();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time < nextAllowedTime)
            return;

        if (collision == null || collision.collider == null || !collision.collider.CompareTag(deathTag))
            return;

        nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
        TeleportToDeathRoom();
    }

    private void TeleportToDeathRoom()
    {
        OVRCameraRig rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null)
            rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig == null)
            return;

        Vector3 targetPos = deathRoomSpawn != null ? deathRoomSpawn.position : fallbackDeathRoomPosition;
        Quaternion targetRot = deathRoomSpawn != null ? deathRoomSpawn.rotation : rig.transform.rotation;

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
            Vector3 eyeOffset = rig.transform.position - rig.centerEyeAnchor.position;
            eyeOffset.y = 0f;
            targetPos += eyeOffset;
        }

        rig.transform.position = targetPos;

        if (applyTargetRotation && deathRoomSpawn != null)
            rig.transform.rotation = targetRot;

        if (restoreController)
            cc.enabled = true;
    }

    private bool IsTouchingDeath(OVRCameraRig rig)
    {
        Vector3 eyePos = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.position : rig.transform.position;
        Vector3 rootPos = rig.transform.position;

        float radius = Mathf.Max(0.01f, overlapCheckRadius);
        float bodyHeight = Mathf.Max(radius * 2.1f, overlapBodyHeight);
        Vector3 capsuleTop = rootPos + Vector3.up * (bodyHeight - radius);
        Vector3 capsuleBottom = rootPos + Vector3.up * radius;

        int hitCount = Physics.OverlapCapsuleNonAlloc(
            capsuleTop,
            capsuleBottom,
            radius,
            overlapHits,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            if (hit != null && hit.CompareTag(deathTag))
                return true;
        }

        // Backup check for thin trigger floors directly under player.
        float rayDistance = Mathf.Max(0.01f, feetRayDistance);
        if (Physics.Raycast(rootPos + Vector3.up * 0.05f, Vector3.down, out RaycastHit floorHit, rayDistance, ~0, QueryTriggerInteraction.Collide))
        {
            if (floorHit.collider != null && floorHit.collider.CompareTag(deathTag))
                return true;
        }

        // Keep a tiny eye-level overlap as final fallback.
        hitCount = Physics.OverlapSphereNonAlloc(eyePos, radius, overlapHits, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            if (hit != null && hit.CompareTag(deathTag))
                return true;
        }

        return false;
    }
}
