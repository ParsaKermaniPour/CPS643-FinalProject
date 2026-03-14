using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sliders : MonoBehaviour
{
    public enum Axis
    {
        Up, Forward
    }

    public Axis axis;

    public Vector3 minValue;

    public Vector3 maxValue;

    public bool ignoreX, ignoreY, ignoreZ;

    public float multiplier = 1;

    [HideInInspector]
    public bool lerp;

    private void Update()
    {

        if (lerp)
        {
            if (axis == Axis.Up)
                transform.Translate(Vector3.up * multiplier * Input.mouseScrollDelta.y * Time.deltaTime);
            else
                transform.Translate(Vector3.forward * multiplier * Input.mouseScrollDelta.y * Time.deltaTime);

            transform.localPosition = new Vector3((ignoreX) ? transform.localPosition.x : Mathf.Clamp(transform.localPosition.x, minValue.x, maxValue.x),
                                                  (ignoreY) ? transform.localPosition.y : Mathf.Clamp(transform.localPosition.y, minValue.y, maxValue.y),
                                                  (ignoreZ) ? transform.localPosition.z : Mathf.Clamp(transform.localPosition.z, minValue.z, maxValue.z));

        }
    }

}
