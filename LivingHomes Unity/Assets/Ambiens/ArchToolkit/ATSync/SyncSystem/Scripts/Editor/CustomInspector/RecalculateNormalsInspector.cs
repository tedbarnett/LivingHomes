using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(RecalculateNormalsComponent))]
[CanEditMultipleObjects]
public class RecalculateNormalsInspector : Editor
{
    private RecalculateNormalsComponent component;

    private bool error;

    private void OnEnable()
    {
        if (component == null)
        {
            component = target as RecalculateNormalsComponent;

            if(component.meshFilter == null)
            {
                error = true;
                EditorUtility.DisplayDialog("Warning","You cannot add this script if the gameobject is without meshFilter","Ok");
                return;
            }

            if(component.meshFilter.sharedMesh == null)
            {
                error = true;
                EditorUtility.DisplayDialog("Warning", "MeshMissing", "Ok");
                return;
            }
        }
        
    }

    public override void OnInspectorGUI()
    {
        if (error)
            return;

        GUILayout.BeginVertical();
        
        this.component.angle = EditorGUILayout.Slider("Angle: ",this.component.angle, 0, 360);
        
        GUILayout.Space(10);

        var color = GUI.backgroundColor;

        GUI.backgroundColor = Color.cyan;

        if (GUILayout.Button("Refresh normals"))
        {
            this.component.RecalculateNormals(this.component.angle);
        }

        GUI.backgroundColor = color;

        GUILayout.EndVertical();
    }
}
