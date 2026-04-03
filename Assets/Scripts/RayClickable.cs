using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add this to any object you want the right-hand ray to click.
/// </summary>
public class RayClickable : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent onClicked;

    [Header("Optional Hover Visual")]
    [Tooltip("Optional object to show while the ray is hovering")]
    public GameObject hoverVisual;

    public void OnRayHoverEnter()
    {
        if (hoverVisual != null)
            hoverVisual.SetActive(true);
    }

    public void OnRayHoverExit()
    {
        if (hoverVisual != null)
            hoverVisual.SetActive(false);
    }

    public void Press()
    {
        onClicked?.Invoke();
    }

    // Optional compatibility if other scripts send this message directly.
    private void OnRayClicked()
    {
        Press();
    }
}
