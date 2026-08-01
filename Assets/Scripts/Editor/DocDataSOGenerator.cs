using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DocDataCreatorWindow : EditorWindow
{
    // Using a list allows us to easily use Unity's default array inspector drawing
    public List<Texture2D> textures = new List<Texture2D>();

    private SerializedObject serializedWindow;
    private SerializedProperty texturesProperty; [MenuItem("Tools/DocData Creator")]
    public static void ShowWindow()
    {
        // Opens the window and sets its title
        GetWindow<DocDataCreatorWindow>("DocData Creator");
    }

    private void OnEnable()
    {
        // Wrap this window in a SerializedObject to easily draw the list in OnGUI
        serializedWindow = new SerializedObject(this);
        texturesProperty = serializedWindow.FindProperty("textures");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create DocDataSO from Textures", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Update and draw the array/list property
        serializedWindow.Update();
        EditorGUILayout.PropertyField(texturesProperty, true);
        serializedWindow.ApplyModifiedProperties();

        EditorGUILayout.Space();

        // Button to execute the generation
        if (GUILayout.Button("Generate Scriptable Objects", GUILayout.Height(30)))
        {
            GenerateScriptableObjects();
        }
    }

    private void GenerateScriptableObjects()
    {
        if (textures.Count == 0)
        {
            Debug.LogWarning("No textures assigned. Please add at least one texture to the list.");
            return;
        }

        // Ensure the directories exist
        EnsureFolderExists("Assets", "Resources");
        EnsureFolderExists("Assets/Resources", "Documents");

        string folderPath = "Assets/Resources/Documents";
        int createdCount = 0;

        for (int i = 0; i < textures.Count; i++)
        {
            Texture2D tex = textures[i];

            if (tex == null)
            {
                Debug.LogWarning($"Element at index {i} is null. Skipping.");
                continue;
            }

            // Create a new instance of the ScriptableObject
            DocDataSO newDoc = ScriptableObject.CreateInstance<DocDataSO>();

            // Assign the texture to the field
            newDoc.DocTexture = tex;

            // Generate a save path based on the texture's name
            string assetPath = $"{folderPath}/{tex.name}_DocData.asset";

            // Ensure we don't overwrite an existing file with the exact same name
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            // Create the physical asset in the project
            AssetDatabase.CreateAsset(newDoc, assetPath);
            createdCount++;
        }

        // Save all changes to disk and refresh the project window
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Successfully created {createdCount} DocDataSO assets in '{folderPath}'.");

        // Optional: clear the list after generating
        textures.Clear();
    }

    // Helper method to safely create folders if they are missing
    private void EnsureFolderExists(string parentFolder, string newFolderName)
    {
        string fullPath = $"{parentFolder}/{newFolderName}";
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }
}