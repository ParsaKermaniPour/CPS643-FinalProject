
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ControlDesk : MonoBehaviour
{
    public GameObject SelectedObject;
    public GameObject HoveredObject;
    
    //Additive materials that will designate oject that is selected/hovered;
    public Material SelectedMaterial;
    public Material HoveredMaterial;

    public ParticleSystem particles;

    public Text leverScreen;
    public Text dialScreen;

    public const string INSTRUCTIONS =
        "Press the TouchPad (Move button) to select something. \n\n" +
        "Press the Application Button (Triangle) to Teleport \n\n" +
        "Hold the trigger to use a control (Red objects)";

    private bool clearHover = true;

    public void Update()
    {
        
        //Clear hovered object if it wasn't selected last frame.
        if(clearHover) Hover(null);
        clearHover = true;
        UpdateInfoScreens();
    }

    public void UpdateInfoScreens()
    {
        if (!SelectedObject)
        {
            leverScreen.text = INSTRUCTIONS;
            dialScreen.text = INSTRUCTIONS;
            return;
        }
        var selPos = SelectedObject.transform.localPosition;

        var leverMessage =
            "SelectedObj:\n  " + SelectedObject.name + " \n\n" +
            "  X Offset: \n    " + selPos.x + " \n" +
            "  Y Offset: \n    " + selPos.y + " \n" +
            "  Z Offset: \n    " + selPos.z;

        leverScreen.text = leverMessage;

        var rb = SelectedObject.GetComponent<Rigidbody>();
        var AngVel = rb.angularVelocity;
        var positionFrozen = rb.constraints == RigidbodyConstraints.FreezePosition;
        float h, s, v;
        Color.RGBToHSV(SelectedObject.GetComponent<MeshRenderer>().material.color, out h, out s, out v);

        var dialMessage =
            "SelectedObj:      \n    " + SelectedObject.name + " \n\n" +
            "  Hue:              \n    " + h + " \n" +
            "  Angular velocity: \n    " + AngVel.z + " \n" +
            "  Is frozen:        \n    " + positionFrozen;

        dialScreen.text = dialMessage;
    }

    // Removes highlight due to selecting
    public void Deselect()
    {
        if(SelectedObject) RemoveMaterial(SelectedObject, SelectedMaterial);
        SelectedObject = null;
    }
    // Removes highlight due to hovering
    public void DeHover()
    {
        if (HoveredObject != null) RemoveMaterial(HoveredObject, HoveredMaterial);
        HoveredObject = null;
    }

    //Hover over given gameobject. Needs to be called every frame that you want to hover. will not work if go is selected.
    public void Hover(GameObject go)
    {
        //We use this to indicate that the hovered object should not be cleared for at least 1 more frame.
        clearHover = false;

        if (go == SelectedObject || go == HoveredObject) return;
        if (HoveredObject != null) RemoveMaterial(HoveredObject, HoveredMaterial);
        if (go != null) AddMaterial(go, HoveredMaterial);
        HoveredObject = go;
    }
    //Select given object. Will de-hover object if it is currently hovered. Deselects previously selected object.
    public void Select(GameObject go)
    {
        if (SelectedObject == go || go == null) return;
        Deselect();
        if (go == HoveredObject) DeHover();
        AddMaterial(go, SelectedMaterial);
        SelectedObject = go;
        
    }

    // Adds a shared material from an obj. Used when you want to render multple materials per obj. Not helpful with multi-part meshes.
    private void AddMaterial(GameObject go, Material mat)
    {

        var mr = go.GetComponentInChildren<MeshRenderer>();
        var materials = mr.sharedMaterials.ToList();
        
        materials.Add(mat);
        mr.sharedMaterials = materials.ToArray();
    }

    // Removes a shared material from an obj. Used when you want to render multple materials per obj. Not helpful with multi-part meshes.
    private void RemoveMaterial(GameObject go, Material mat)
    {
        var mr = go.GetComponentInChildren<MeshRenderer>();
        var materials = mr.sharedMaterials.ToList();
        
        materials.Remove(mat);
        mr.sharedMaterials = materials.ToArray();
    }

    //These methods set the local position of an object from its pivot.
    public void SetX(float x)
    {
        if (!SelectedObject) return;
        Vector3 pos = SelectedObject.transform.localPosition;
        pos.x = x;
        SelectedObject.transform.localPosition = pos;
    }
    public void SetY(float y)
    {
        if (!SelectedObject) return;
        Vector3 pos = SelectedObject.transform.localPosition;
        pos.y = y;
        SelectedObject.transform.localPosition = pos;
    }
    public void SetZ(float z)
    {
        if (!SelectedObject) return;
        Vector3 pos = SelectedObject.transform.localPosition;
        pos.z = z;
        SelectedObject.transform.localPosition = pos;
    }
    //Sets the color of the obj to a random color.
    public void RandomizeHue()
    {
        if (!SelectedObject) return;
        var mr = SelectedObject.GetComponent<MeshRenderer>();
        mr.material.color = Random.ColorHSV(0, 1, 1, 1, 1, 1);
         
    }
    //Changes the hue by the delta amount. Value is ignored.
    public void ChangeHue(float value, float delta)
    {
        if (!SelectedObject) return;
        var mr = SelectedObject.GetComponent<MeshRenderer>();
        if (mr.material.color == Color.white) RandomizeHue();
        float hue, sat, val;
        Color.RGBToHSV(mr.material.color, out hue, out sat, out val);
        hue += delta / 360;
        mr.material.color = Color.HSVToRGB(hue, sat, val);
    }

    //Sets the angular velocity of the selected object around its local z axis.
    public void SetAngularVelocity(float value)
    {
        if (!SelectedObject) return;
        var rb = SelectedObject.GetComponent<Rigidbody>();
        rb.angularVelocity = new Vector3(0, 0, value/140);
    }

    //Unlocks the selected object from its pivot
    public void Unattach()
    {
        if (!SelectedObject) return;
        var rb = SelectedObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.None;
    }

    //Resets a rigidbody to its original position and locks it.
    public void ResetRB()
    {
        if (!SelectedObject) return;
        var rb = SelectedObject.GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezePosition;
        SelectedObject.transform.localPosition = Vector3.zero;
        SelectedObject.transform.localRotation = Quaternion.identity;

    }

    // Starts a particle shower in the given location relative to the topdown camera.
    public void Shower(float x, float z)
    {
        // This is how we modify particle parameters.
        var emisionModule = particles.emission;
        emisionModule.enabled = true;
        //The topdown camera which is the partcle's parent.
        var cam = particles.GetComponentInParent<Camera>();
        //Viewport coordinates are from 0-1 and represent a point on the screen. this converts them into a world position relative to the camera.
        var point = cam.ViewportToWorldPoint(new Vector3(x+.5f, z+.5f, 0));
        particles.transform.position = point;
    }

    // Stop particle Shower
    public void StopShower()
    {
        var emisionModule = particles.emission;
        emisionModule.enabled = false;
    }
}
