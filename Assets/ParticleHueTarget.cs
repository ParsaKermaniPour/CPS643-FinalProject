using UnityEngine;

public class ParticleHueTarget : MonoBehaviour
{
    [Tooltip("Assign one or more ParticleSystems. If empty, auto-finds in children.")]
    public ParticleSystem[] systems;

    [Header("Color")]
    [Range(0f, 1f)] public float saturation = 1f;
    [Range(0f, 1f)] public float value = 1f;

    [Tooltip("Optional: multiply brightness (HDR-ish) without changing hue.")]
    public float intensity = 1f;

    [Tooltip("If enabled, also drives Color Over Lifetime (gradient).")]
    public bool setColorOverLifetime = false;

    public bool restartOnHueChange = true;

    void Awake()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
    }

    // Hook your Dial's UnityEvent<float> to this

    public void SetHue01(float t)
    {
        t = Mathf.Repeat(t, 1f);
        Color c = Color.HSVToRGB(t, saturation, value) * intensity;

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            Debug.Log("Setting hue on " + ps.name + " to " + c);

            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(c);

            if (setColorOverLifetime)
            {
                var col = ps.colorOverLifetime;
                col.enabled = true;

                Gradient g = new Gradient();
                g.SetKeys(
                    new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );

                col.color = new ParticleSystem.MinMaxGradient(g);
            }

            // Make the change show immediately
            ps.Clear(true);
            ps.Play(true);
        }
    }

    
}
