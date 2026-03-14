using UnityEngine;

/// <summary>
/// Visual indicator cube that shows combination lock progress and spins on unlock.
/// Attach this to a Cube GameObject placed in front of the player/safe.
///
/// Step progress: changes the cube color (red → yellow → green)
/// Unlock: cube starts spinning fast in all axes
/// </summary>
public class UnlockIndicator : MonoBehaviour
{
    [Header("Spin Settings")]
    [Tooltip("Rotation speed (degrees per second) once unlocked")]
    public float spinSpeed = 180f;

    [Header("Step Colors")]
    [Tooltip("Default color — no steps done yet")]
    public Color colorDefault = Color.red;

    [Tooltip("Color after step 1 is complete")]
    public Color colorStep1 = new Color(1f, 0.5f, 0f); // orange

    [Tooltip("Color after step 2 is complete")]
    public Color colorStep2 = Color.yellow;

    [Tooltip("Color when fully unlocked")]
    public Color colorUnlocked = Color.green;

    private bool isSpinning = false;
    private Renderer cubeRenderer;

    void Awake()
    {
        cubeRenderer = GetComponent<Renderer>();
        SetColor(colorDefault);
    }

    void Update()
    {
        if (isSpinning)
        {
            // Spin on all axes for a flashy unlock effect
            transform.Rotate(
                spinSpeed * Time.deltaTime,
                spinSpeed * 1.3f * Time.deltaTime,
                spinSpeed * 0.7f * Time.deltaTime,
                Space.Self
            );
        }
    }

    /// <summary>Called by CombinationLock when a step is completed (not yet unlocked)</summary>
    public void ShowStepProgress(int completedSteps)
    {
        switch (completedSteps)
        {
            case 1: SetColor(colorStep1); break;
            case 2: SetColor(colorStep2); break;
        }
    }

    /// <summary>Called by CombinationLock when the full combination is solved</summary>
    public void TriggerUnlock()
    {
        SetColor(colorUnlocked);
        isSpinning = true;
    }

    /// <summary>Reset back to default state</summary>
    public void Reset()
    {
        isSpinning = false;
        transform.rotation = Quaternion.identity;
        SetColor(colorDefault);
    }

    private void SetColor(Color color)
    {
        if (cubeRenderer == null) return;
        // Directly set the material color — works with all render pipelines
        cubeRenderer.material.color = color;
    }
}
