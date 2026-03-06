using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{

    RaycastHit hit;
    Ray ray;
    GameObject hitG;
    public float interactionRange;
    private Vector3 offset;
    Sliders[] slider;
    HalfCircleSliders[] halfCircleSliders;
    private void Start()
    {
        slider = FindObjectsOfType<Sliders>();
        halfCircleSliders = FindObjectsOfType<HalfCircleSliders>();
    }

    private void Update()
    {

        ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction, Color.red);

        if (Physics.Raycast(ray, out hit, interactionRange))
        {

            hitG = hit.collider.gameObject;

            if (Input.GetKeyDown(KeyCode.E))
            {
                foreach (var item in slider)
                {
                    if (item.lerp && item != hitG.GetComponent<Sliders>())
                        item.lerp = false;
                }

                foreach (var item in halfCircleSliders)
                {
                    if (item.lerp && item != hitG.GetComponent<HalfCircleSliders>())
                        item.lerp = false;
                }

                if (hitG.GetComponent<Sliders>())
                {
                    hitG.GetComponent<Sliders>().lerp = !hitG.GetComponent<Sliders>().lerp;
                }

                if (hitG.GetComponent<Buttons>())
                {
                    hitG.GetComponent<Buttons>().lerp = !hitG.GetComponent<Buttons>().lerp;
                }

                if (hitG.GetComponent<TouchScreen>())
                {
                    hitG.GetComponent<TouchScreen>().ChangeColorAndText();
                }

                if (hitG.GetComponent<DialsLeversAndFlipSwitches>())
                {
                    hitG.GetComponent<DialsLeversAndFlipSwitches>().lerp = !hitG.GetComponent<DialsLeversAndFlipSwitches>().lerp;
                }

                if (hitG.GetComponent<HalfCircleSliders>())
                {
                    hitG.GetComponent<HalfCircleSliders>().lerp = !hitG.GetComponent<HalfCircleSliders>().lerp;
                }
            }
        }
    }
}
