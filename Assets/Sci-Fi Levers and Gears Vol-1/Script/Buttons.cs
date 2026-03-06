
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Buttons : MonoBehaviour
{

    public enum ButtonType
    {
        PressType, ClickType
    }

    public float pressLimitValue;

    public float multiplier;

    public ButtonType buttonType;

    public float timeToReturnBack = 0.25f;

    float timer;

    [HideInInspector]
    public bool lerp;

    Vector3 _intialPos, _endPos;

    private void Start()
    {
        _intialPos = transform.localPosition;
        _endPos = new Vector3(transform.localPosition.x, pressLimitValue, transform.localPosition.z);
    }
    void Update()
    {
        if (buttonType == ButtonType.PressType)
        {
            if (lerp)
                transform.localPosition = Vector3.Lerp(transform.localPosition, _endPos, multiplier * Time.deltaTime);
            else
                transform.localPosition = Vector3.Lerp(transform.localPosition, _intialPos, multiplier * Time.deltaTime);

        }
        else
        {
            if (lerp)
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _endPos, multiplier * Time.deltaTime);
                timer += Time.deltaTime;
                if (timer >= timeToReturnBack)
                {
                    lerp = false;
                    timer = 0;
                }
            }
            else
            {
                transform.localPosition = Vector3.Lerp(transform.localPosition, _intialPos, multiplier * Time.deltaTime);
            }

        }


    }
}
