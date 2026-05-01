using UnityEngine;

public class MapSelectionManager : MonoBehaviour
{
    [SerializeField] private Transform[] spawnLocations = new Transform[2];
    [SerializeField] private GameObject[] floorObjects;
    [SerializeField] private GameObject[] spawnBlockerObjects = new GameObject[2];
    [SerializeField] private GameObject[] objectiveBlockerObjects = new GameObject[2];
    [SerializeField] private GameObject[] exitBlockerObjects = new GameObject[2];
    [SerializeField] private Transform rigRoot;
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private GameObject playButtonObject;
    
    [Header("Button Renderers")]
    [SerializeField] private Renderer[] spawnButtonRenderers = new Renderer[2];
    [SerializeField] private Renderer[] objectiveButtonRenderers = new Renderer[2];
    [SerializeField] private Renderer[] exitButtonRenderers = new Renderer[2];
    [SerializeField] private Renderer floorUpRenderer;
    [SerializeField] private Renderer floorDownRenderer;
    [SerializeField] private Renderer playButtonRenderer;
    
    [Header("Button Colors")]
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color unselectedColor = Color.red;
    [SerializeField] private Color floorButtonColor = Color.yellow;
    [SerializeField] private Color playButtonColor = Color.black;
    
    private bool quickSelectActive = false;
    private int quickSelectedSpawn = -1;
    private int quickSelectedObjective = -1;
    private int quickSelectedExit = -1;

    private int selectedSpawnIndex = -1;
    private int selectedObjectiveIndex = -1;
    private int selectedExitIndex = -1;
    private int currentFloorIndex = 0;

    void Awake()
    {
        if (rigRoot == null || centerEyeAnchor == null)
        {
            OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                rigRoot = rig.transform;
                centerEyeAnchor = rig.centerEyeAnchor;
            }
        }

        InitializeFloors();
    }

    void InitializeFloors()
    {
        if (floorObjects == null || floorObjects.Length == 0)
            return;

        for (int i = 0; i < floorObjects.Length; i++)
        {
            if (floorObjects[i] != null)
                floorObjects[i].SetActive(i == currentFloorIndex);
        }

        foreach (var blocker in spawnBlockerObjects)
            if (blocker != null)
                blocker.SetActive(false);

        foreach (var blocker in objectiveBlockerObjects)
            if (blocker != null)
                blocker.SetActive(false);

        foreach (var blocker in exitBlockerObjects)
            if (blocker != null)
                blocker.SetActive(false);

        InitializeButtonColors();
        UpdatePlayButton();
    }

    void InitializeButtonColors()
    {
        if (floorUpRenderer != null)
            floorUpRenderer.material.color = floorButtonColor;
        if (floorDownRenderer != null)
            floorDownRenderer.material.color = floorButtonColor;

        for (int i = 0; i < spawnButtonRenderers.Length; i++)
            if (spawnButtonRenderers[i] != null)
                spawnButtonRenderers[i].material.color = unselectedColor;

        for (int i = 0; i < objectiveButtonRenderers.Length; i++)
            if (objectiveButtonRenderers[i] != null)
                objectiveButtonRenderers[i].material.color = unselectedColor;

        for (int i = 0; i < exitButtonRenderers.Length; i++)
            if (exitButtonRenderers[i] != null)
                exitButtonRenderers[i].material.color = unselectedColor;
    }

    public void OnButtonPressed(MapSelectionButton.ButtonIdentity identity)
    {
        if (quickSelectActive && identity != MapSelectionButton.ButtonIdentity.Play)
            return;

        switch (identity)
        {
            case MapSelectionButton.ButtonIdentity.SpawnOne:
                selectedSpawnIndex = 0;
                UpdateSpawnButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.SpawnTwo:
                selectedSpawnIndex = 1;
                UpdateSpawnButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.FloorUp:
                FloorUp();
                break;
            case MapSelectionButton.ButtonIdentity.FloorDown:
                FloorDown();
                break;
            case MapSelectionButton.ButtonIdentity.ObjectiveOne:
                selectedObjectiveIndex = 0;
                UpdateObjectiveButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.ObjectiveTwo:
                selectedObjectiveIndex = 1;
                UpdateObjectiveButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.ExitOne:
                selectedExitIndex = 0;
                UpdateExitButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.ExitTwo:
                selectedExitIndex = 1;
                UpdateExitButtonColors();
                UpdatePlayButton();
                break;
            case MapSelectionButton.ButtonIdentity.Play:
                if (quickSelectActive)
                {
                    selectedSpawnIndex = quickSelectedSpawn;
                    selectedObjectiveIndex = quickSelectedObjective;
                    selectedExitIndex = quickSelectedExit;
                }
                if (selectedSpawnIndex >= 0 && selectedObjectiveIndex >= 0 && selectedExitIndex >= 0)
                {
                    ConfigureBlockers();
                    if (quickSelectActive)
                    {
                        TeleportPlayerToFixedPosition(new Vector3(0f, 0f, -30f), Quaternion.Euler(0, 90, 0));
                    }
                    else
                    {
                        TeleportPlayerToSpawn();
                    }
                }
                break;
        }
    }

    private void TeleportPlayerToFixedPosition(Vector3 position, Quaternion rotation)
    {
        if (rigRoot == null)
            return;

        CharacterController characterController = rigRoot.GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
            rigRoot.position = position;
            rigRoot.rotation = rotation;
            characterController.enabled = true;
        }
        else
        {
            rigRoot.position = position;
            rigRoot.rotation = rotation;
        }
    }

    public void QuickSelectRobbery(QuickSelectButton.RobberyType type)
    {
        Debug.Log("QuickSelectRobbery called with type: " + type + " | objective will be: " + (type == QuickSelectButton.RobberyType.Potion ? 1 : 0));
        quickSelectActive = true;
        quickSelectedSpawn = 0;
        if (type == QuickSelectButton.RobberyType.Potion)
        {
            quickSelectedObjective = 1; // Safe puzzle room
        }
        else
        {
            quickSelectedObjective = 0; // Keypad/ray gun puzzle room
        }
        quickSelectedExit = Random.Range(0, 2);

        selectedSpawnIndex = quickSelectedSpawn;
        selectedObjectiveIndex = quickSelectedObjective;
        selectedExitIndex = quickSelectedExit;
        UpdateSpawnButtonColors();
        UpdateObjectiveButtonColors();
        UpdateExitButtonColors();
        UpdatePlayButton();
    }

    public void DisableQuickSelect()
    {
        quickSelectActive = false;
        QuickSelectButton.ResetQuickSelect();
    }

    private void FloorUp()
    {
        if (currentFloorIndex > 0)
        {
            if (floorObjects[currentFloorIndex] != null)
                floorObjects[currentFloorIndex].SetActive(false);

            currentFloorIndex--;

            if (floorObjects[currentFloorIndex] != null)
                floorObjects[currentFloorIndex].SetActive(true);
        }
    }

    private void FloorDown()
    {
        if (floorObjects == null || floorObjects.Length == 0)
            return;

        if (currentFloorIndex < floorObjects.Length - 1)
        {
            if (floorObjects[currentFloorIndex] != null)
                floorObjects[currentFloorIndex].SetActive(false);

            currentFloorIndex++;

            if (floorObjects[currentFloorIndex] != null)
                floorObjects[currentFloorIndex].SetActive(true);
        }
    }

    private void ConfigureBlockers()
    {
        for (int i = 0; i < spawnBlockerObjects.Length; i++)
        {
            if (spawnBlockerObjects[i] != null)
                spawnBlockerObjects[i].SetActive(i != selectedSpawnIndex);
        }

        for (int i = 0; i < objectiveBlockerObjects.Length; i++)
        {
            if (objectiveBlockerObjects[i] != null)
                objectiveBlockerObjects[i].SetActive(i != selectedObjectiveIndex);
        }

        for (int i = 0; i < exitBlockerObjects.Length; i++)
        {
            if (exitBlockerObjects[i] != null)
                exitBlockerObjects[i].SetActive(i != selectedExitIndex);
        }
    }

    private void UpdatePlayButton()
    {
        bool allSelectionsComplete = selectedSpawnIndex >= 0 && selectedObjectiveIndex >= 0 && selectedExitIndex >= 0;
        if (playButtonObject != null)
            playButtonObject.SetActive(allSelectionsComplete);

        if (allSelectionsComplete && playButtonRenderer != null)
            playButtonRenderer.material.color = playButtonColor;
    }

    private void UpdateSpawnButtonColors()
    {
        for (int i = 0; i < spawnButtonRenderers.Length; i++)
        {
            if (spawnButtonRenderers[i] != null)
            {
                Color color = (i == selectedSpawnIndex) ? selectedColor : unselectedColor;
                spawnButtonRenderers[i].material.color = color;
            }
        }
    }

    private void UpdateObjectiveButtonColors()
    {
        for (int i = 0; i < objectiveButtonRenderers.Length; i++)
        {
            if (objectiveButtonRenderers[i] != null)
            {
                Color color = (i == selectedObjectiveIndex) ? selectedColor : unselectedColor;
                objectiveButtonRenderers[i].material.color = color;
            }
        }
    }

    private void UpdateExitButtonColors()
    {
        for (int i = 0; i < exitButtonRenderers.Length; i++)
        {
            if (exitButtonRenderers[i] != null)
            {
                Color color = (i == selectedExitIndex) ? selectedColor : unselectedColor;
                exitButtonRenderers[i].material.color = color;
            }
        }
    }

    private void TeleportPlayerToSpawn()
    {
        if (rigRoot == null || centerEyeAnchor == null || spawnLocations[selectedSpawnIndex] == null)
            return;

        Transform spawnTransform = spawnLocations[selectedSpawnIndex];
        Vector3 eyeOffset = centerEyeAnchor.position - rigRoot.position;
        eyeOffset.y = 0f;

        Vector3 targetRigPosition = spawnTransform.position - eyeOffset;
        targetRigPosition.y = rigRoot.position.y;

        CharacterController characterController = rigRoot.GetComponent<CharacterController>();
        if (characterController != null && characterController.enabled)
        {
            characterController.enabled = false;
            rigRoot.position = targetRigPosition;
            characterController.enabled = true;
        }
        else
        {
            rigRoot.position = targetRigPosition;
        }
    }
}