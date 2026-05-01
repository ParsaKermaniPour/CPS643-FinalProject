using UnityEngine;

public class MapSelectionButton : MonoBehaviour
{
    public enum ButtonIdentity { SpawnOne, SpawnTwo, FloorUp, FloorDown, ObjectiveOne, ObjectiveTwo, ExitOne, ExitTwo, Play }
    
    [SerializeField] private ButtonIdentity identity;
    [SerializeField] private MapSelectionManager manager;

    void Awake()
    {
        if (manager == null)
            manager = FindFirstObjectByType<MapSelectionManager>();

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Fingertip"))
            return;

            if (manager != null)
            {
                manager.DisableQuickSelect();
                manager.OnButtonPressed(identity);
            }
    }
}
