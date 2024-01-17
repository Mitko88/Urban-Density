using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(FieldOfViewRays))]
public class VisibilityEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var myScript = (FieldOfViewRays)target;
        if (GUILayout.Button("Run"))
        {
            myScript.Run();
        }
    }
}

