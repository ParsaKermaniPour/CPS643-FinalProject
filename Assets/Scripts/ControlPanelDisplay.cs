using UnityEngine;
using TMPro;

public class ControlPanelDisplay : MonoBehaviour
{
    [Header("Text Labels (assign in Inspector)")]
    public TextMeshProUGUI sliderLabel;
    public TextMeshProUGUI dialLabel;
    public TextMeshProUGUI switchLabel;
    public TextMeshProUGUI leverLabel;

    public void SetSlider(float t)
    {
        if (sliderLabel)
            sliderLabel.text = "Emission: " + Mathf.RoundToInt(t * 100f) + "%";
    }

    public void SetDial(float t)
    {
        if (dialLabel)
            dialLabel.text = "Hue: " + Mathf.RoundToInt(t * 360f) + "°";
    }

    public void SetSwitch(bool on)
    {
        if (switchLabel)
            switchLabel.text = "Particles: " + (on ? "ON" : "OFF");
    }

    public void SetLever(float t)
    {
        if (leverLabel)
            leverLabel.text = "Spin: " + Mathf.RoundToInt(t * 100f) + "%";
    }
}
