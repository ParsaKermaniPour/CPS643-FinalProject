using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    GameObject character;
    float sensitivity;
    Vector2 mouseLook;    

	// Use this for initialization
	void Start () {
        character = this.transform.parent.gameObject;
        
	}
	
	// Update is called once per frame
	void Update () {
        
        sensitivity = character.GetComponent<Movement>().lookSensitivity;
        float Horizontal = Input.GetAxis("Mouse X");
        float Vertical = Input.GetAxis("Mouse Y");
        Vector2 look = new Vector2(Horizontal,Vertical);
        mouseLook += look * sensitivity;

        transform.localRotation = Quaternion.AngleAxis(-mouseLook.y,Vector3.right);
        character.transform.localRotation = Quaternion.AngleAxis(mouseLook.x,character.transform.up);

	}
}
