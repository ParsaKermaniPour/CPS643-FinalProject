using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Spawns number labels in a fixed ring around the dial.
/// Attach this to the DIAL ROOT (same object as DialInteractable).
/// The numbers are placed on a child GameObject that does NOT rotate,
/// so they stay completely still while the knob spins.
/// </summary>
public class DialNumberRing : MonoBehaviour
{
    [Header("Ring Layout")]
    [Tooltip("If enabled, radius/height are computed from the dial mesh/collider bounds")]
    public bool autoFitToDial = true;

    [Tooltip("Radius of the number ring in meters — match it to the dial's physical edge")]
    public float ringRadius = 0.18f;

    [Tooltip("Height offset above the dial center")]
    public float heightOffset = 0.015f;

    [Tooltip("Push labels slightly inward/outward from detected dial edge (meters)")]
    public float edgePadding = -0.005f;

    [Tooltip("Lift labels slightly above the dial top surface (meters)")]
    public float surfaceOffset = 0.0015f;

    [Tooltip("Push labels toward the dial front (local forward) so they are not buried by the rim")]
    public float frontOffset = 0.02f;

    [Tooltip("How many numbers to show around the ring (e.g. 5 shows 0, 20, 40, 60, 80)")]
    public int visibleNumberCount = 5;

    [Tooltip("Total numbers on the dial — must match DialInteractable.totalNumbers")]
    public int totalNumbers = 100;

    [Header("Text Appearance")]
    [Tooltip("Font size of the number labels")]
    public float fontSize = 0.012f;

    [Tooltip("Color of the numbers")]
    public Color textColor = Color.white;

    [Tooltip("Force labels to render white at runtime regardless of inspector textColor")]
    public bool forceWhiteText = true;

    [Tooltip("Face numbers inward toward center, or outward away from center")]
    public bool faceInward = false;

    [Tooltip("When enabled, labels use a depth-tested material so they are hidden by geometry")]
    public bool depthTestLabels = true;

    void Start()
    {
        RemoveAllNumberRings();
    }

    private void RemoveAllNumberRings()
    {
        Transform root = transform.root != null ? transform.root : transform;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (t == null || t.name != "NumberRing_Static")
                continue;

            if (Application.isPlaying)
                Destroy(t.gameObject);
#if UNITY_EDITOR
            else
                DestroyImmediate(t.gameObject);
#endif
        }
    }

    void BuildRing()
    {
        // Keep the number ring out of the rotating dial transform hierarchy.
        // This preserves the dial's initial orientation while the dial itself spins.
        Transform host = transform.parent;
        Transform existing = host != null ? host.Find("NumberRing_Static") : null;

        GameObject ringParent = existing != null ? existing.gameObject : new GameObject("NumberRing_Static");
        ringParent.transform.SetParent(host, true);
        ringParent.transform.position = transform.position;
        ringParent.transform.rotation = transform.rotation;

        float effectiveRadius = ringRadius;
        float effectiveHeight = heightOffset;
        if (autoFitToDial)
        {
            GetAutoFittedLayout(out effectiveRadius, out effectiveHeight);
        }

        // Rebuild labels cleanly if this runs more than once.
        for (int i = ringParent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(ringParent.transform.GetChild(i).gameObject);
        }

        int step = totalNumbers / visibleNumberCount;
        Material labelMaterial = null;
        Color effectiveTextColor = forceWhiteText ? Color.white : textColor;

        for (int i = 0; i < visibleNumberCount; i++)
        {
            int number = (i * step) % totalNumbers;

            // Calculate angle — start at top (0 degrees = 12 o'clock), go clockwise
            float angle = (number / (float)totalNumbers) * 360f;
            float rad = angle * Mathf.Deg2Rad;

            // Position along ring
            float x = Mathf.Sin(rad) * effectiveRadius;
            float z = Mathf.Cos(rad) * ringRadius;
            z = Mathf.Cos(rad) * effectiveRadius;
            Vector3 localPos = new Vector3(x, effectiveHeight, z) + Vector3.forward * frontOffset;

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
            tm.color = effectiveTextColor;
            tm.fontStyle = FontStyle.Bold;

            if (depthTestLabels)
            {
                if (labelMaterial == null)
                    labelMaterial = CreateDepthTestedLabelMaterial(tm.font, effectiveTextColor);

                MeshRenderer renderer = labelObj.GetComponent<MeshRenderer>();
                if (renderer != null && labelMaterial != null)
                {
                    renderer.sharedMaterial = labelMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }
        }
    }

    private Material CreateDepthTestedLabelMaterial(Font font, Color color)
    {
        Shader shader = Shader.Find("GUI/Text Shader");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");

        Material material = null;
        if (shader != null)
            material = new Material(shader);

        if (material == null)
            return null;

        if (font != null && font.material != null && font.material.mainTexture != null)
            material.mainTexture = font.material.mainTexture;

        if (material.HasProperty("_Color"))
            material.color = color;

        if (material.HasProperty("_ZTest"))
            material.SetInt("_ZTest", (int)CompareFunction.LessEqual);

        return material;
    }

    private void GetAutoFittedLayout(out float fittedRadius, out float fittedHeight)
    {
        if (!TryGetDialBounds(out Bounds worldBounds))
        {
            fittedRadius = ringRadius;
            fittedHeight = heightOffset;
            return;
        }

        Vector3 localCenter = transform.InverseTransformPoint(worldBounds.center);
        Vector3 extents = worldBounds.extents;

        fittedRadius = Mathf.Max(0.001f, Mathf.Max(extents.x, extents.z) + edgePadding);
        fittedHeight = localCenter.y + extents.y + surfaceOffset;
    }

    private bool TryGetDialBounds(out Bounds combinedBounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        combinedBounds = new Bounds(transform.position, Vector3.zero);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer.transform.name == "NumberRing_Static" || renderer.transform.IsChildOf(transform) == false)
                continue;

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col.transform.name == "NumberRing_Static" || col.transform.IsChildOf(transform) == false)
                continue;

            if (!hasBounds)
            {
                combinedBounds = col.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(col.bounds);
            }
        }

        return hasBounds;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            RemoveAllNumberRings();
    }

    // Draw the ring in editor so you can preview radius before pressing play
    void OnDrawGizmosSelected()
    {
        float effectiveRadius = ringRadius;
        float effectiveHeight = heightOffset;
        if (autoFitToDial)
        {
            GetAutoFittedLayout(out effectiveRadius, out effectiveHeight);
        }

        Gizmos.color = Color.yellow;
        int segments = 64;
        Vector3 prev = transform.TransformPoint(new Vector3(effectiveRadius, effectiveHeight, 0));
        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 next = transform.TransformPoint(new Vector3(
                Mathf.Cos(angle) * effectiveRadius,
                effectiveHeight,
                Mathf.Sin(angle) * effectiveRadius));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}
