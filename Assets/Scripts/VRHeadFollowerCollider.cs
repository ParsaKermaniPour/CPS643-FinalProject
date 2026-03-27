using UnityEngine;

public class VRHeadFollowerCollider : MonoBehaviour
{
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, -0.25f, 0f);

    private void LateUpdate()
    {
        if (centerEyeAnchor == null) return;

        transform.position = centerEyeAnchor.position + positionOffset;
    }
}
