using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HalfCircleSliders : MonoBehaviour
{


    public float maxX;
    public float minX;

    public float multiplier;

    [HideInInspector]
    public bool lerp;

    float rotationX;

    private void Start()
    {
        rotationX = transform.localRotation.x;
        transform.localRotation = Quaternion.Euler(rotationX, transform.localRotation.y, transform.localRotation.z);
    }

    void Update()
    {
        if (!lerp)
            return;

        rotationX -= Input.mouseScrollDelta.y * multiplier;
        rotationX = Mathf.Clamp(rotationX, minX, maxX);
        transform.localRotation = Quaternion.Euler(rotationX, transform.localRotation.y, transform.localRotation.z);
    }
}
