using UnityEngine;

public class ParticleSettingsTarget : MonoBehaviour
{
    public ParticleSystem[] systems;

    [Header("Emission Rate (particles/sec)")]
    public float minRate = 0f;
    public float maxRate = 150f;

    [Header("Optional: Start Speed")]
    public bool alsoControlSpeed = false;
    public float minSpeed = 0.2f;
    public float maxSpeed = 2.5f;

    float[] defaultRates;
    float[] defaultSpeeds;

    void Awake()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);

        defaultRates = new float[systems.Length];
        defaultSpeeds = new float[systems.Length];

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            var em = ps.emission;
            defaultRates[i] = em.rateOverTime.constant;

            var main = ps.main;
            defaultSpeeds[i] = main.startSpeed.constant;
        }
    }

    public void SetEmissionRate01(float t)
    {
        t = Mathf.Clamp01(t);
        float rate = Mathf.Lerp(minRate, maxRate, t);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            var em = ps.emission;
            em.rateOverTime = rate;

            if (alsoControlSpeed)
            {
                var main = ps.main;
                main.startSpeed = speed;
            }
        }
    }

    public void ResetToDefaults()
    {
        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            var em = ps.emission;
            em.rateOverTime = defaultRates[i];

            var main = ps.main;
            main.startSpeed = defaultSpeeds[i];
        }
    }
}
