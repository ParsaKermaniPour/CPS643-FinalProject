using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipSwitches : MonoBehaviour {
    

    public float multiplier;

    [HideInInspector]
    public bool lerp;

    public Vector3 rotationValue;

    Vector3 _intialVector;

    private void Start()
    {
        _intialVector = transform.localEulerAngles;
    }
    void Update()
    {
        if (lerp)
        {
            transform.localRotation= Quaternion.Lerp(transform.localRotation, Quaternion.Euler(rotationValue), multiplier*Time.deltaTime);
        }
        else
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, Quaternion.Euler(_intialVector), multiplier * Time.deltaTime);
        }
    }
}
