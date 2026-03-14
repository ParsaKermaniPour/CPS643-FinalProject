using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeistObjectiveTracker : MonoBehaviour
{
    public CombinationLock combinationLock;
    public PickupGrabbable goldPickup;
    public TMP_Text objectiveText;
    public bool autoCreateTopLeftUI = true;
    public Vector2 topLeftOffset = new Vector2(24f, -24f);
    public bool preferVRWorldSpace = true;
    public Vector3 vrHudLocalPosition = new Vector3(-0.22f, 0.14f, 0.7f);
    public Vector3 vrHudLocalEuler = Vector3.zero;
    public float vrHudScale = 0.0012f;

    public string unlockSafeText = "Unlock safe";
    public string takeGoldText = "Take gold";
    public string getOutText = "GET OUT!!";

    private enum ObjectiveStage
    {
        UnlockSafe,
        TakeGold,
        GetOut
    }

    private ObjectiveStage currentStage = ObjectiveStage.UnlockSafe;

    void Start()
    {
        if (combinationLock == null)
            combinationLock = FindFirstObjectByType<CombinationLock>();

        if (autoCreateTopLeftUI && objectiveText == null)
            CreateTopLeftUI();

        if (combinationLock != null)
            combinationLock.onUnlocked.AddListener(OnSafeUnlocked);

        RefreshText();
    }

    void OnDestroy()
    {
        if (combinationLock != null)
            combinationLock.onUnlocked.RemoveListener(OnSafeUnlocked);
    }

    void Update()
    {
        if (currentStage == ObjectiveStage.TakeGold && goldPickup != null && goldPickup.IsGrabbed)
        {
            currentStage = ObjectiveStage.GetOut;
            RefreshText();
        }
    }

    public void OnSafeUnlocked()
    {
        if (currentStage != ObjectiveStage.UnlockSafe)
            return;

        currentStage = ObjectiveStage.TakeGold;
        RefreshText();
    }

    private void RefreshText()
    {
        if (objectiveText == null)
            return;

        string value = currentStage == ObjectiveStage.UnlockSafe ? unlockSafeText :
                       currentStage == ObjectiveStage.TakeGold ? takeGoldText :
                       getOutText;

        objectiveText.text = "Objective: " + value;
    }

    private void CreateTopLeftUI()
    {
        GameObject canvasObj = new GameObject("HeistObjectiveCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Transform vrAnchor = FindCenterEyeAnchor();
        bool useVRWorldSpace = preferVRWorldSpace && vrAnchor != null;

        if (useVRWorldSpace)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.transform.SetParent(vrAnchor, false);
            canvasObj.transform.localPosition = vrHudLocalPosition;
            canvasObj.transform.localRotation = Quaternion.Euler(vrHudLocalEuler);
            canvasObj.transform.localScale = Vector3.one * vrHudScale;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(900f, 120f);
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
        }

        GameObject textObj = new GameObject("ObjectiveText");
        textObj.transform.SetParent(canvasObj.transform, false);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 38;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;

        RectTransform rect = tmp.rectTransform;
        if (useVRWorldSpace)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, 0f);
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
            rect.sizeDelta = new Vector2(900f, 120f);
        }

        objectiveText = tmp;
    }

    private Transform FindCenterEyeAnchor()
    {
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
        if (rig != null && rig.centerEyeAnchor != null)
            return rig.centerEyeAnchor;

        Camera cam = Camera.main;
        return cam != null ? cam.transform : null;
    }
}
