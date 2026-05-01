using UnityEngine;

public class ClueNote : MonoBehaviour
{
    public string letter;
    public string digit;

    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        
        // Accept hand/controller/finger contacts
        if (!other.CompareTag("Fingertip") && 
            !other.CompareTag("LeftController") && 
            !other.CompareTag("RightController") &&
            !other.CompareTag("Player")) return;

        collected = true;
        if (WristNotepad.Instance != null)
            WristNotepad.Instance.SetClue(letter, digit);
            
        Debug.Log("Clue collected: " + letter + " = " + digit);
    }
}