using UnityEngine;

public class ParticleToggleTarget : MonoBehaviour
{
    [Header("Particle Systems")]
    [Tooltip("Assign one or more ParticleSystems to toggle. If empty, will auto-find in children.")]
    public ParticleSystem[] systems;

    [Header("Behavior")]
    [Tooltip("If true, clears particles when turning off (no lingering).")]
    public bool clearOnStop = true;

    [Tooltip("If true, disables the GameObject(s) holding the ParticleSystem when off.")]
    public bool disableGameObjectWhenOff = false;

    void Awake()
    {
        if (systems == null || systems.Length == 0)
            systems = GetComponentsInChildren<ParticleSystem>(includeInactive: true);
    }

    public void SetParticles(bool on)
    {
        if (systems == null) return;

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (!ps) continue;

            if (disableGameObjectWhenOff)
            {
                ps.gameObject.SetActive(on);
                if (on) ps.Play(true);
                continue;
            }

            if (on)
            {
                ps.Play(true);
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (clearOnStop) ps.Clear(true);
            }
        }
    }

    // Optional convenience for UnityEvents that use float 0/1
    public void SetParticles01(float t) => SetParticles(t >= 0.5f);
}
