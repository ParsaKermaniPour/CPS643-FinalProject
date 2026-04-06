using NavKeypad;
using UnityEngine;

public class DisableLasers : MonoBehaviour
{
    [SerializeField] private Keypad keypad;
    [SerializeField] private GameObject lasersRoot;
    [SerializeField] private bool disableRootObject = true;

    private void Awake()
    {
        if (keypad == null)
            keypad = FindFirstObjectByType<Keypad>();

        if (lasersRoot == null)
            lasersRoot = GameObject.Find("lasers");
    }

    private void OnEnable()
    {
        if (keypad != null)
            keypad.OnAccessGranted.AddListener(OnKeypadSolved);
    }

    private void OnDisable()
    {
        if (keypad != null)
            keypad.OnAccessGranted.RemoveListener(OnKeypadSolved);
    }

    private void OnKeypadSolved()
    {
        if (lasersRoot == null)
        {
            Debug.LogWarning("[DisableLasers] lasersRoot is not assigned or found.");
            return;
        }

        if (disableRootObject)
        {
            lasersRoot.SetActive(false);
            return;
        }

        for (int i = 0; i < lasersRoot.transform.childCount; i++)
        {
            lasersRoot.transform.GetChild(i).gameObject.SetActive(false);
        }
    }
}
