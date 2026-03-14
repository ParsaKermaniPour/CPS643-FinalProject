using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchScreen : MonoBehaviour
{
    public Text textElementToChange;

    public GameObject screen;

    public string[] switchTexts;

    public Color[] colors;

    public float brightness;

    int incCol, incTxt;

    void Start()
    {
        if (screen.GetComponent<Renderer>() != null)
        {
            screen.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            screen.GetComponent<Renderer>().material.SetColor("_EmissionColor", colors[incCol] * brightness);
        }
    }

    public void ChangeColorAndText()
    {


        if (screen.GetComponent<Renderer>() != null)
        {
            incCol++;
            incCol = incCol % colors.Length;
            screen.GetComponent<Renderer>().material.SetColor("_EmissionColor", colors[incCol] * brightness);
        }

        if (textElementToChange != null)
        {
            incTxt++;
            incTxt = incTxt % switchTexts.Length;

            textElementToChange.text = switchTexts[incTxt];

        }

    }

}
