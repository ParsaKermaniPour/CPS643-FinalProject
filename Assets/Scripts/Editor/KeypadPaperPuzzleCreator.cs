using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using NavKeypad;

public static class KeypadPaperPuzzleCreator
{
    [MenuItem("GameObject/Keypad/Create Paper Puzzle Notes", false, 30)]
    public static void CreatePaperPuzzleNotes()
    {
        System.Random rng = new System.Random();
        Vector3 origin = Selection.activeTransform != null ? Selection.activeTransform.position : Vector3.zero;
        Transform parent = CreateOrReuseParent(origin);

        ClearExistingNotes(parent);

        var cluePool = new List<ClueData>
        {
            new ClueData(
                "Clue A",
                "I have 5 edges but no corners of time. Name me, and you have the digit.",
                5
            ),
            new ClueData(
                "Clue B",
                "Take the square root of 256, then divide by the number of legs on a standard chair.",
                4
            ),
            new ClueData(
                "Clue C",
                "A die opposite to 1 is this number.",
                6
            ),
            new ClueData(
                "Clue D",
                "In binary, 111 becomes this in decimal.",
                7
            ),
            new ClueData(
                "Clue E",
                "Take a triangle's interior angle sum and divide by the number of degrees in one-tenth of a full turn.",
                5
            )
        };

        // Keep all clues visible, but only use a random subset for the code.
        // The remaining visible clue acts as a red herring.
        const int requiredClueCount = 4;
        List<ClueData> orderedForCode = cluePool
            .OrderBy(_ => rng.Next())
            .Take(Mathf.Clamp(requiredClueCount, 1, cluePool.Count))
            .ToList();
        int generatedCode = int.Parse(string.Concat(orderedForCode.Select(c => c.Digit.ToString())));

        List<NoteData> notes = cluePool
            .Select(c => new NoteData(c.Title, c.Body))
            .ToList();

        string orderText = string.Join(" -> ", orderedForCode.Select(c => c.Title.Replace("Clue ", "")));
        notes.Add(new NoteData("Order", orderText));

        const float spacingX = 0.40f;
        const float spacingY = 0.28f;

        for (int i = 0; i < notes.Count; i++)
        {
            int row = i / 3;
            int col = i % 3;
            Vector3 position = origin + new Vector3(col * spacingX, -row * spacingY, 0f);
            CreatePaperNote(parent, $"PaperNote_{i + 1}", position, notes[i]);
        }

        ApplyCodeToKeypads(generatedCode);

        Selection.activeTransform = parent;
        Debug.Log($"Created {notes.Count} paper puzzle notes under KeypadPaperNotes. Generated keypad code: {generatedCode}");
    }

    private static void ClearExistingNotes(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static Transform CreateOrReuseParent(Vector3 origin)
    {
        GameObject parent = GameObject.Find("KeypadPaperNotes");
        if (parent == null)
        {
            parent = new GameObject("KeypadPaperNotes");
            parent.transform.position = origin;
            Undo.RegisterCreatedObjectUndo(parent, "Create keypad paper notes parent");
        }

        return parent.transform;
    }

    private static void CreatePaperNote(Transform parent, string name, Vector3 worldPosition, NoteData data)
    {
        GameObject noteRoot = new GameObject(name);
        noteRoot.transform.SetParent(parent, true);
        noteRoot.transform.position = worldPosition;
        noteRoot.transform.rotation = parent.rotation;
        Undo.RegisterCreatedObjectUndo(noteRoot, "Create paper note root");

        GameObject canvasGo = new GameObject("Sheet");
        canvasGo.transform.SetParent(noteRoot.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform sheetRt = canvasGo.GetComponent<RectTransform>();
        sheetRt.sizeDelta = new Vector2(220f, 140f);
        sheetRt.localScale = Vector3.one * 0.0015f;
        sheetRt.localPosition = Vector3.zero;
        sheetRt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-2.5f, 2.5f));

        var background = canvasGo.AddComponent<Image>();
        background.color = new Color(0.96f, 0.90f, 0.56f, 1f);

        var shadow = canvasGo.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        shadow.effectDistance = new Vector2(2f, -2f);

        GameObject tapeGo = new GameObject("TapeStrip");
        tapeGo.transform.SetParent(canvasGo.transform, false);
        RectTransform tapeRt = tapeGo.AddComponent<RectTransform>();
        tapeRt.anchorMin = new Vector2(0.30f, 0.92f);
        tapeRt.anchorMax = new Vector2(0.70f, 1.03f);
        tapeRt.offsetMin = Vector2.zero;
        tapeRt.offsetMax = Vector2.zero;
        tapeRt.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-6f, 6f));
        var tapeImage = tapeGo.AddComponent<Image>();
        tapeImage.color = new Color(1f, 0.97f, 0.80f, 0.62f);

        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(canvasGo.transform, false);
        RectTransform titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0.77f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(12f, -2f);
        titleRt.offsetMax = new Vector2(-12f, -8f);

        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = data.Title.ToUpperInvariant();
        titleTmp.fontSize = 15f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.16f, 0.11f, 0.05f, 1f);
        titleTmp.alignment = TextAlignmentOptions.TopLeft;
        titleTmp.textWrappingMode = TextWrappingModes.NoWrap;

        GameObject bodyGo = new GameObject("Body");
        bodyGo.transform.SetParent(canvasGo.transform, false);
        RectTransform bodyRt = bodyGo.AddComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 0.78f);
        bodyRt.offsetMin = new Vector2(12f, 10f);
        bodyRt.offsetMax = new Vector2(-12f, -4f);

        var bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
        bodyTmp.text = data.Body;
        bodyTmp.fontSize = 12f;
        bodyTmp.fontStyle = FontStyles.Italic;
        bodyTmp.lineSpacing = 4f;
        bodyTmp.color = new Color(0.19f, 0.15f, 0.09f, 1f);
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        bodyTmp.overflowMode = TextOverflowModes.Overflow;
    }

    private static void ApplyCodeToKeypads(int code)
    {
        Keypad[] keypads = UnityEngine.Object.FindObjectsByType<Keypad>(FindObjectsSortMode.None);
        if (keypads.Length == 0)
        {
            Debug.LogWarning("No Keypad component found in open scenes. Notes were created, but no keypad code was updated.");
            return;
        }

        foreach (Keypad keypad in keypads)
        {
            Undo.RecordObject(keypad, "Set keypad combo");
            SerializedObject so = new SerializedObject(keypad);
            SerializedProperty combo = so.FindProperty("keypadCombo");
            if (combo != null)
            {
                combo.intValue = code;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(keypad);
            }
        }
    }

    private readonly struct NoteData
    {
        public readonly string Title;
        public readonly string Body;

        public NoteData(string title, string body)
        {
            Title = title;
            Body = body;
        }
    }

    private readonly struct ClueData
    {
        public readonly string Title;
        public readonly string Body;
        public readonly int Digit;

        public ClueData(string title, string body, int digit)
        {
            Title = title;
            Body = body;
            Digit = digit;
        }
    }
}