using TMPro;
using UnityEngine;

public class WristNotepad : MonoBehaviour
{
    public static WristNotepad Instance;
    [SerializeField] private TMP_Text displayText;

    private string a, b, c, d, e;

    void Awake()
    {
        Instance = this;
        UpdateDisplay();
    }

    public void SetClue(string letter, string digit)
    {
        switch (letter.ToUpper())
        {
            case "A": a = digit; break;
            case "B": b = digit; break;
            case "C": c = digit; break;
            case "D": d = digit; break;
            case "E": e = digit; break;
        }
        UpdateDisplay();
    }

    public void ResetClues()
    {
        a = b = c = d = e = null;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        displayText.text = $"A: {a ?? "?"}\nB: {b ?? "?"}\nC: {c ?? "?"}\nD: {d ?? "?"}\nE: {e ?? "?"}";
    }
}