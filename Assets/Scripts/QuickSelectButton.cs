using UnityEngine;
using UnityEngine.UI;

public class QuickSelectButton : MonoBehaviour
{
    public enum RobberyType { Potion, RayGun }
    public RobberyType robberyType;
    public Color selectedColor = Color.green;
    public Color unselectedColor = Color.red;
    public Image buttonImage;
    public MeshRenderer buttonRenderer;

    [Header("Doors")]
    public GameObject potionDoor;   // drag the safe room door blocker here
    public GameObject rayGunDoor;   // drag the keypad room door blocker here

    private MapSelectionManager manager;
    private static QuickSelectButton activeQuickSelect;

    void Awake()
    {
        manager = FindFirstObjectByType<MapSelectionManager>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        if (buttonRenderer == null)
            buttonRenderer = GetComponent<MeshRenderer>();
        SetSelected(false);
    }

    public void OnButtonPressed()
    {
        if (activeQuickSelect != null && activeQuickSelect != this)
            activeQuickSelect.SetSelected(false);
        activeQuickSelect = this;
        SetSelected(true);

        // Directly control doors
        if (potionDoor != null) potionDoor.SetActive(robberyType != RobberyType.Potion);
        if (rayGunDoor != null) rayGunDoor.SetActive(robberyType != RobberyType.RayGun);

        if (manager != null)
            manager.QuickSelectRobbery(robberyType);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Fingertip"))
            return;
        OnButtonPressed();
    }

    public void SetSelected(bool selected)
    {
        if (buttonImage != null)
            buttonImage.color = selected ? selectedColor : unselectedColor;
        if (buttonRenderer != null && buttonRenderer.material != null)
            buttonRenderer.material.color = selected ? selectedColor : unselectedColor;
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