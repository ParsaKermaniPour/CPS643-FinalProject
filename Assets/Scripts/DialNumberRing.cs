using UnityEngine;

/// <summary>
/// Spawns number labels in a fixed ring around the dial.
/// Attach this to the DIAL ROOT (same object as DialInteractable).
/// The numbers are placed on a child GameObject that does NOT rotate,
/// so they stay completely still while the knob spins.
/// </summary>
public class DialNumberRing : MonoBehaviour
{
    [Header("Ring Layout")]
    [Tooltip("Radius of the number ring in meters — match it to the dial's physical edge")]
    public float ringRadius = 0.18f;

    [Tooltip("Height offset above the dial center")]
    public float heightOffset = 0.015f;

    [Tooltip("How many numbers to show around the ring (e.g. 5 shows 0, 20, 40, 60, 80)")]
    public int visibleNumberCount = 5;

    [Tooltip("Total numbers on the dial — must match DialInteractable.totalNumbers")]
    public int totalNumbers = 100;

    [Header("Text Appearance")]
    [Tooltip("Font size of the number labels")]
    public float fontSize = 0.012f;

    [Tooltip("Color of the numbers")]
    public Color textColor = Color.white;

    [Tooltip("Face numbers inward toward center, or outward away from center")]
    public bool faceInward = false;

    void Start()
    {
        BuildRing();
    }

    void BuildRing()
    {
        // Create a static parent that is a sibling-level child of Dial root
        // but is NOT the rotating knob — so numbers never move
        GameObject ringParent = new GameObject("NumberRing_Static");
        ringParent.transform.SetParent(transform, false);
        // Lock it to world-space so rotation of siblings doesn't affect it
        ringParent.transform.localPosition = Vector3.zero;
        ringParent.transform.localRotation = Quaternion.identity;

        int step = totalNumbers / visibleNumberCount;

        for (int i = 0; i < visibleNumberCount; i++)
        {
            int number = (i * step) % totalNumbers;

            // Calculate angle — start at top (0 degrees = 12 o'clock), go clockwise
            float angle = (number / (float)totalNumbers) * 360f;
            float rad = angle * Mathf.Deg2Rad;

            // Position along ring
            float x = Mathf.Sin(rad) * ringRadius;
            float z = Mathf.Cos(rad) * ringRadius;
            Vector3 localPos = new Vector3(x, heightOffset, z);

            // Create a label
            GameObject labelObj = new GameObject($"Label_{number}");
            labelObj.transform.SetParent(ringParent.transform, false);
            labelObj.transform.localPosition = localPos;

            // Face the label — rotate it to read outward or inward
            Vector3 lookDir = faceInward ? -localPos : localPos;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                labelObj.transform.localRotation = Quaternion.LookRotation(lookDir);
                // Tilt flat so numbers face upward (readable from above like a real dial)
                labelObj.transform.Rotate(90f, 0f, 0f, Space.Self);
            }

            // Add TextMesh component
            TextMesh tm = labelObj.AddComponent<TextMesh>();
            tm.text = number.ToString();
            tm.fontSize = 100;
            tm.characterSize = fontSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = textColor;
            tm.fontStyle = FontStyle.Bold;
        }
    }

#if UNITY_EDITOR
    // Draw the ring in editor so you can preview radius before pressing play
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        int segments = 64;
        Vector3 prev = transform.position + new Vector3(ringRadius, heightOffset, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = transform.position + new Vector3(
                Mathf.Cos(angle) * ringRadius,
                heightOffset,
                Mathf.Sin(angle) * ringRadius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
