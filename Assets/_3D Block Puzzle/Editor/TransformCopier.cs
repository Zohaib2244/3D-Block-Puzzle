using UnityEngine;
using UnityEditor;

public class TransformCopier : EditorWindow
{
    private enum CopyMode { Local, World }
    private static CopyMode copyMode = CopyMode.World;

    private static bool copyPosition = true;
    private static bool copyRotation = true;
    private static bool copyScale = false;

    // Data for Transform
    private static Vector3 copiedPosition;
    private static Quaternion copiedRotation;
    private static Vector3 copiedScale;

    // Data for RectTransform
    private static Vector2 copiedAnchoredPosition;
    private static Vector2 copiedSizeDelta;
    private static Vector2 copiedAnchorMin;
    private static Vector2 copiedAnchorMax;
    private static Vector2 copiedPivot;

    private static bool isRectTransform = false;
    private static bool hasCopy = false;

    // --- New: Rotation Shortcut Settings ---
    private static float rotateDegrees = 90f;
    private static bool rotateClockwise = true;

    [MenuItem("NuttyTools/Transform Copy Paste")]
    public static void ShowWindow()
    {
        GetWindow<TransformCopier>("Transform Copy Paste");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copy Mode", EditorStyles.boldLabel);
        copyMode = (CopyMode)EditorGUILayout.EnumPopup("Mode", copyMode);

        GUILayout.Label("Copy Components", EditorStyles.boldLabel);
        copyPosition = EditorGUILayout.Toggle("Position", copyPosition);
        copyRotation = EditorGUILayout.Toggle("Rotation", copyRotation);
        copyScale = EditorGUILayout.Toggle("Scale", copyScale);

        GUILayout.Space(10);

        if (GUILayout.Button("Copy Selected Transform (Cmd+Alt+C)"))
        {
            CopySelected();
        }
        if (GUILayout.Button("Paste To Selected Transform (Cmd+Alt+V)"))
        {
            PasteToSelected();
        }

        GUILayout.Space(20);
        GUILayout.Label("Quick Rotate", EditorStyles.boldLabel);
        rotateDegrees = EditorGUILayout.FloatField("Degrees", rotateDegrees);
        rotateClockwise = EditorGUILayout.Toggle("Clockwise", rotateClockwise);

        if (GUILayout.Button("Rotate Selected (Ctrl+Alt+R)"))
        {
            RotateSelected();
        }
    }

    [MenuItem("NuttyTools/Transform Copy Paste/Copy %&c")]
    private static void CopySelected()
    {
        var t = Selection.activeTransform;
        if (t == null)
        {
            Debug.LogWarning("No object selected.");
            return;
        }

        isRectTransform = t is RectTransform;
        if (isRectTransform)
        {
            var rect = t as RectTransform;
            copiedAnchoredPosition = rect.anchoredPosition;
            copiedSizeDelta = rect.sizeDelta;
            copiedAnchorMin = rect.anchorMin;
            copiedAnchorMax = rect.anchorMax;
            copiedPivot = rect.pivot;
        }

        if (copyMode == CopyMode.Local)
        {
            copiedPosition = t.localPosition;
            copiedRotation = t.localRotation;
            copiedScale = t.localScale;
        }
        else
        {
            copiedPosition = t.position;
            copiedRotation = t.rotation;
            copiedScale = t.lossyScale;
        }

        hasCopy = true;
        EditorGUIUtility.systemCopyBuffer = "TransformCopied";
        Debug.Log($"{(isRectTransform ? "RectTransform" : "Transform")} copied ({copyMode})");
    }

    [MenuItem("NuttyTools/Transform Copy Paste/Paste %&v")]
    private static void PasteToSelected()
    {
        if (!hasCopy || Selection.transforms.Length == 0)
        {
            Debug.LogWarning("Nothing copied or no objects selected.");
            return;
        }

        foreach (var t in Selection.transforms)
        {
            Undo.RecordObject(t, "Paste Transform");

            if (isRectTransform && t is RectTransform rect)
            {
                if (copyPosition) rect.anchoredPosition = copiedAnchoredPosition;
                rect.sizeDelta = copiedSizeDelta;
                rect.anchorMin = copiedAnchorMin;
                rect.anchorMax = copiedAnchorMax;
                rect.pivot = copiedPivot;

                if (copyMode == CopyMode.Local)
                {
                    if (copyPosition) rect.localPosition = copiedPosition;
                    if (copyRotation) rect.localRotation = copiedRotation;
                    if (copyScale) rect.localScale = copiedScale;
                }
                else
                {
                    if (copyPosition) rect.position = copiedPosition;
                    if (copyRotation) rect.rotation = copiedRotation;
                    if (copyScale) rect.localScale = copiedScale; // lossyScale can't be set directly
                }
            }
            else
            {
                if (copyMode == CopyMode.Local)
                {
                    if (copyPosition) t.localPosition = copiedPosition;
                    if (copyRotation) t.localRotation = copiedRotation;
                    if (copyScale) t.localScale = copiedScale;
                }
                else
                {
                    if (copyPosition) t.position = copiedPosition;
                    if (copyRotation) t.rotation = copiedRotation;
                    if (copyScale) t.localScale = copiedScale; // lossyScale can't be set directly
                }
            }

            EditorUtility.SetDirty(t);
        }
        Debug.Log($"{(isRectTransform ? "RectTransform" : "Transform")} pasted to {Selection.transforms.Length} object(s) ({copyMode})");
    }

    // --- New: Rotate Selected Shortcut ---
    [MenuItem("NuttyTools/Transform Copy Paste/Rotate %r")]
    private static void RotateSelected()
    {
        if (Selection.transforms.Length == 0)
        {
            Debug.LogWarning("No objects selected to rotate.");
            return;
        }

        float angle = rotateClockwise ? rotateDegrees : -rotateDegrees;

        foreach (var t in Selection.transforms)
        {
            Undo.RecordObject(t, "Rotate Transform");
            t.Rotate(Vector3.up, angle, Space.Self);
            EditorUtility.SetDirty(t);
        }
        Debug.Log($"Rotated {Selection.transforms.Length} object(s) by {angle} degrees.");
    }
}