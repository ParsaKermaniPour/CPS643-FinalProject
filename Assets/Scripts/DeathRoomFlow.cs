using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DeathRoomFlow : MonoBehaviour
{
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

    private float nextAllowedTime;

    void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
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
            TeleportRigTo(deathRoomSpawn, fallbackDeathRoomPosition);
        else
            TeleportRigTo(planningRoomSpawn, fallbackPlanningRoomPosition);
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
            targetPos += eyeToRigOffset;
        }

        rig.transform.position = targetPos;
        if (applyTargetRotation && targetTransform != null)
            rig.transform.rotation = targetRot;

        if (restoreController)
            cc.enabled = true;
    }
}
