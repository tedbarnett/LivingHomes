using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ATSettings
{
    public static bool setNormalToZero;
    public static bool saveMesh;
}

public class ATSyncPreferences
{
    private static bool prefsLoaded;
    
    [PreferenceItem("ATSync")]
    public static void PreferencesGUI()
    {
        // Load the preferences
        if (!prefsLoaded)
        {
            ambiens.utils.loader.RuntimeMeshLoader.Instance.saveMesh = EditorPrefs.GetBool("ATSyncSaveMesh", false);
            ATSettings.setNormalToZero = EditorPrefs.GetBool("ATSyncNormalZero", false);

            prefsLoaded = true;
        }

        // Preferences GUI
        ATSettings.saveMesh = EditorGUILayout.Toggle(new GUIContent("Save mesh","Save mesh into the disk during the import"), ATSettings.saveMesh);
        ATSettings.setNormalToZero = EditorGUILayout.Toggle(new GUIContent("Set normal to 0 degree", "Set normal to zero degree during the import, this can avoid some artifacts"), ATSettings.setNormalToZero);

        // Save the preferences
        if (GUI.changed)
        {
            EditorPrefs.SetBool("ATSyncSaveMesh", ATSettings.saveMesh);
            EditorPrefs.SetBool("ATSyncNormalZero", ATSettings.setNormalToZero);

        }
    }
}
