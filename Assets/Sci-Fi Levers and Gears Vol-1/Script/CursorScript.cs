using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorScript : MonoBehaviour {

    public Texture2D cursorImage;

    [Range(0,1000)]
    public float cursorWidth, cursorHeight;
        
    void OnGUI()
    {
        
        Cursor.visible = false;
        float xMin = (Screen.width / 2) - (cursorWidth/ 2);
        float yMin = (Screen.height / 2) - (cursorHeight / 2);
        GUI.DrawTexture(new Rect(xMin, yMin, cursorWidth,cursorHeight ), cursorImage);
    }


}
