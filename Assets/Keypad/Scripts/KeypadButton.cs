using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace NavKeypad
{
    public class KeypadButton : Interactable
    {
        [Header("Value")]
        [SerializeField] private string value;
        [Header("Button Animation Settings")]
        [SerializeField] private float bttnspeed = 0.1f;
        [SerializeField] private float moveDist = 0.0025f;
        [SerializeField] private float buttonPressedTime = 0.1f;
        [Header("Component References")]
        [SerializeField] private Keypad keypad;

        [Header("Touch Filter")]
        [SerializeField] private bool requirePokeTip = true;
        [SerializeField] private float triggerCooldown = 0.18f;


        public void PressButton()
        {
            Debug.Log("PressButton called for: " + value + ", moving: " + moving);
            if (!moving)
            {
                keypad.AddInput(value);
                StartCoroutine(MoveSmooth());
            }
        }

        // VR Controller touch support
        public override void OnTouchEnter(OVRController ctrl)
        {
            // Intentionally ignored for keypad precision.
            // Key presses should come from tip-trigger OnTriggerEnter only.
        }

        // Fallback path: trigger directly from controller colliders.
        private void OnTriggerEnter(Collider other)
        {
            if (!CanTriggerFromCollider(other))
                return;

            if (pokeInside)
                return;

            if (Time.time < lastTriggerTime + triggerCooldown)
                return;

            pokeInside = true;
            lastTriggerTime = Time.time;

            Debug.Log("Button trigger enter from: " + other.name + " value: " + value);
            PressButton();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!CanTriggerFromCollider(other))
                return;

            pokeInside = false;
        }

        // Called by RightHandRaySelector via SendMessage.
        private void OnRayClicked()
        {
            PressButton();
        }

        private bool moving;
        private float lastTriggerTime;
        private bool pokeInside;

        private bool CanTriggerFromCollider(Collider other)
        {
            if (other == null)
                return false;

            if (requirePokeTip)
                return other.GetComponent<KeypadPokeTip>() != null || other.GetComponentInParent<KeypadPokeTip>() != null;

            return IsControllerCollider(other);
        }

        private static bool IsControllerCollider(Collider other)
        {
            if (other == null)
                return false;

            if (other.GetComponentInParent<OVRController>() != null)
                return true;

            if (other.CompareTag("LeftController") || other.CompareTag("RightController"))
                return true;

            string n = other.name;
            return n.Contains("Controller") || n.Contains("HandAnchor");
        }

        private IEnumerator MoveSmooth()
        {

            moving = true;
            Vector3 startPos = transform.localPosition;
            Vector3 endPos = transform.localPosition + new Vector3(0, 0, moveDist);

            float elapsedTime = 0;
            while (elapsedTime < bttnspeed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / bttnspeed);

                transform.localPosition = Vector3.Lerp(startPos, endPos, t);

                yield return null;
            }
            transform.localPosition = endPos;
            yield return new WaitForSeconds(buttonPressedTime);
            startPos = transform.localPosition;
            endPos = transform.localPosition - new Vector3(0, 0, moveDist);

            elapsedTime = 0;
            while (elapsedTime < bttnspeed)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / bttnspeed);

                transform.localPosition = Vector3.Lerp(startPos, endPos, t);

                yield return null;
            }
            transform.localPosition = endPos;

            moving = false;
        }
    }
}