using UnityEngine;

public class DeathTagListener : MonoBehaviour
{
    [Header("Detection")]
    public string deathTag = "Death";

    [Tooltip("Extra reliability: check for overlap with Death-tagged triggers every frame")]
    public bool useOverlapCheck = true;

    [Tooltip("Radius for overlap check around player head/body")]
    public float overlapCheckRadius = 0.2f;

    [Header("Teleport")]
    public Transform deathRoomSpawn;
    public Vector3 fallbackDeathRoomPosition = new Vector3(0f, 2f, 0f);
    public bool applyTargetRotation = true;
    public bool disableCharacterControllerDuringTeleport = true;

    [Header("Safety")]
    public float cooldownSeconds = 0.75f;

    private float nextAllowedTime;
    private readonly Collider[] overlapHits = new Collider[16];

    private void Update()
    {
        if (!useOverlapCheck || Time.time < nextAllowedTime)
            return;

        OVRCameraRig rig = GetComponentInParent<OVRCameraRig>();
        if (rig == null)
            rig = FindFirstObjectByType<OVRCameraRig>();

        if (rig == null)
            return;

        Vector3 origin = rig.centerEyeAnchor != null ? rig.centerEyeAnchor.position : rig.transform.position;
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            Mathf.Max(0.01f, overlapCheckRadius),
            overlapHits,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = overlapHits[i];
            if (hit == null)
                continue;

            if (!hit.CompareTag(deathTag))
                continue;

            nextAllowedTime = Time.time + Mathf.Max(0f, cooldownSeconds);
            TeleportToDeathRoom();
            break;
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
}
