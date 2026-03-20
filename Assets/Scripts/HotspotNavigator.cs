using UnityEngine;

public class HotspotNavigator : MonoBehaviour
{
    public Transform rigRoot;
    public Transform centerEyeAnchor;
    public Transform[] hotspots;
    public OVRInput.Button cycleButton = OVRInput.Button.Two;
    public bool teleportToHotspot = false;

    private int currentIndex = -1;

    void Start()
    {
        if (rigRoot == null)
            rigRoot = transform;

        if (centerEyeAnchor == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
                centerEyeAnchor = rig.centerEyeAnchor;
        }
    }

    void Update()
    {
        if (OVRInput.GetDown(cycleButton))
            GoToNextHotspot();
    }

    public void GoToNextHotspot()
    {
        if (hotspots == null || hotspots.Length == 0)
            return;

        for (int i = 0; i < hotspots.Length; i++)
        {
            currentIndex = (currentIndex + 1) % hotspots.Length;
            if (hotspots[currentIndex] != null)
            {
                MoveOrRotateTo(hotspots[currentIndex]);
                break;
            }
        }
    }

    private void MoveOrRotateTo(Transform hotspot)
    {
        if (hotspot == null || rigRoot == null || centerEyeAnchor == null)
            return;

        if (teleportToHotspot)
        {
            Vector3 eyeOffset = centerEyeAnchor.position - rigRoot.position;
            eyeOffset.y = 0f;

            // Keep the rig's current floor height and only snap X/Z to the hotspot.
            Vector3 targetRigPosition = hotspot.position - eyeOffset;
            targetRigPosition.y = rigRoot.position.y;

            CharacterController characterController = rigRoot.GetComponent<CharacterController>();
            if (characterController != null && characterController.enabled)
            {
                characterController.enabled = false;
                rigRoot.position = targetRigPosition;
                characterController.enabled = true;
            }
            else
            {
                rigRoot.position = targetRigPosition;
            }
        }

        Vector3 toHotspot = hotspot.position - centerEyeAnchor.position;
        toHotspot.y = 0f;

        if (toHotspot.sqrMagnitude < 0.0001f)
            return;

        float targetYaw = Mathf.Atan2(toHotspot.x, toHotspot.z) * Mathf.Rad2Deg;
        Vector3 euler = rigRoot.eulerAngles;
        rigRoot.rotation = Quaternion.Euler(euler.x, targetYaw, euler.z);
    }
}
