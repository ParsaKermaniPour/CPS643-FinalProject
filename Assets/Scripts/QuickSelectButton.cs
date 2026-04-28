using UnityEngine;
using UnityEngine.UI;

public class QuickSelectButton : MonoBehaviour
{
    public enum RobberyType { Potion, RayGun }
    public RobberyType robberyType;
    public Color selectedColor = Color.green;
    public Color unselectedColor = Color.red;
    public Image buttonImage; // Assign in inspector if using UI Image, or swap for MeshRenderer if 3D

    private MapSelectionManager manager;
    private static QuickSelectButton activeQuickSelect;

    void Awake()
    {
        manager = FindFirstObjectByType<MapSelectionManager>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        SetSelected(false);
    }

    public void OnButtonPressed()
    {
        if (activeQuickSelect != null && activeQuickSelect != this)
            activeQuickSelect.SetSelected(false);
        activeQuickSelect = this;
        SetSelected(true);
        if (manager != null)
            manager.QuickSelectRobbery(robberyType);
    }

    public void SetSelected(bool selected)
    {
        if (buttonImage != null)
            buttonImage.color = selected ? selectedColor : unselectedColor;
    }

    public static void ResetQuickSelect()
    {
        if (activeQuickSelect != null)
        {
            activeQuickSelect.SetSelected(false);
            activeQuickSelect = null;
        }
    }
}
