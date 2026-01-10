using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DialogueData dialogueData = (DialogueData)target;

        DrawDefaultInspector();
    }
}

