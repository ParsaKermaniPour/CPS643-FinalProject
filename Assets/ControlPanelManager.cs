using UnityEngine;

public class ControlPanelManager : MonoBehaviour
{
    [Header("Main Target")]
    public GameObject target;

    public float maxSpinSpeed = 180f; // degrees per second
    float currentSpinSpeed;

    [Header("Display")]
    public ControlPanelDisplay display;

    [Header("Particle Targets")]
    public ParticleToggleTarget particleTarget;
    public ParticleHueTarget particleHueTarget;
    public ParticleToggleTarget particleToggleTarget;
    public ParticleSettingsTarget particleSettingsTarget; // (optional) see below

    // Dial -> particle hue
    public void OnDialHueChanged(float t)
    {
        if (particleHueTarget)
        {
           particleHueTarget.SetHue01(t);
        }
        if (display) display.SetDial(t);
    }

    // Slider -> particle emission rate (0..1)
    public void OnSliderParticleRateChanged(float t)
    {
        if (particleSettingsTarget)
        {
            Debug.Log("Particle rate " + t);
            particleSettingsTarget.SetEmissionRate01(t);
        }
        if (display) display.SetSlider(t);
    }

    // Switch -> particles on/off
    public void OnParticlesToggled(bool on)
    {
        if (particleToggleTarget) particleToggleTarget.SetParticles(on);
        if (display) display.SetSwitch(on);
    }

    // Button -> particle and main target reset
    public void OnResetPressed()
    {
        if (particleToggleTarget) particleToggleTarget.SetParticles(false);

        // optional: reset particle settings to defaults
        if (particleSettingsTarget) particleSettingsTarget.ResetToDefaults();

        if (!target) return;

        currentSpinSpeed = 0;
    }

    void Update()
    {
        if (!target) return;

        target.transform.Rotate(Vector3.up, currentSpinSpeed * Time.deltaTime, Space.World);
    }

    // Hinge Slider -> onValueChanged(float 0..1) 
    public void OnRotationSpeedChanged(float t)
    {
        currentSpinSpeed = t * maxSpinSpeed;
        if (display) display.SetLever(t);
    }

 }
