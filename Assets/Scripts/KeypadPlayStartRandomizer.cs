using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NavKeypad;
using TMPro;
using UnityEngine;

public class KeypadPlayStartRandomizer : MonoBehaviour
{
    [SerializeField] private Transform notesRoot;
    [SerializeField] private int requiredClueCount = 4;

    private static readonly (string id, int digit)[] ClueDigits =
    {
        ("A", 5),
        ("B", 4),
        ("C", 6),
        ("D", 7),
        ("E", 5)
    };

    private static readonly FieldInfo KeypadComboField = typeof(Keypad)
        .GetField("keypadCombo", BindingFlags.Instance | BindingFlags.NonPublic);

    private void Awake()
    {
        if (notesRoot == null)
        {
            GameObject root = GameObject.Find("KeypadPaperNotes");
            if (root != null)
                notesRoot = root.transform;
        }
    }

    private void Start()
    {
        RandomizeForThisPlay();
    }

    [ContextMenu("Randomize Now")]
    public void RandomizeForThisPlay()
    {
        var rng = new System.Random(Guid.NewGuid().GetHashCode());
        var pool = ClueDigits.ToList();

        int count = Mathf.Clamp(requiredClueCount, 1, pool.Count);

        // For the common 4-of-5 case, exclude exactly one random clue so each clue
        // has an equal chance to be the one left out.
        if (count == pool.Count - 1)
        {
            int excludedIndex = rng.Next(pool.Count);
            pool.RemoveAt(excludedIndex);
            Shuffle(pool, rng);
        }
        else
        {
            Shuffle(pool, rng);
            pool = pool.Take(count).ToList();
        }

        var selected = pool;

        string orderText = string.Join(" -> ", selected.Select(x => x.id));
        int code = int.Parse(string.Concat(selected.Select(x => x.digit.ToString())));

        TryUpdateOrderNote(orderText);
        ApplyCodeToAllKeypads(code);

        Debug.Log($"[KeypadPlayStartRandomizer] Order: {orderText}, Code: {code}");
    }

    private void TryUpdateOrderNote(string orderText)
    {
        if (notesRoot == null)
        {
            Debug.LogWarning("[KeypadPlayStartRandomizer] Notes root not found.");
            return;
        }

        foreach (Transform note in notesRoot)
        {
            TMP_Text[] texts = note.GetComponentsInChildren<TMP_Text>(true);
            TMP_Text title = texts.FirstOrDefault(t => t.name.Equals("Title", StringComparison.OrdinalIgnoreCase));
            TMP_Text body = texts.FirstOrDefault(t => t.name.Equals("Body", StringComparison.OrdinalIgnoreCase));

            if (title == null || body == null)
                continue;

            if (title.text.Trim().Equals("ORDER", StringComparison.OrdinalIgnoreCase))
            {
                body.text = orderText;
                return;
            }
        }

        Debug.LogWarning("[KeypadPlayStartRandomizer] ORDER note not found.");
    }

    private static void ApplyCodeToAllKeypads(int code)
    {
        if (KeypadComboField == null)
        {
            Debug.LogWarning("[KeypadPlayStartRandomizer] Could not access keypadCombo field.");
            return;
        }

        Keypad[] keypads = FindObjectsByType<Keypad>(FindObjectsSortMode.None);
        foreach (Keypad keypad in keypads)
        {
            KeypadComboField.SetValue(keypad, code);
        }
    }

    private static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
