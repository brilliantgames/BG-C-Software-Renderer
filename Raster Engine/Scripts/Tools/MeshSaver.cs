using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MeshSaver
{
    /// <summary>
    /// Saves the given mesh to a file.
    /// In the Unity Editor, it saves as an Asset.
    /// In a Build, it saves as an OBJ in persistentDataPath.
    /// </summary>
    public static void SaveMesh(Mesh mesh, string defaultName = "SavedMesh")
    {
        if (mesh == null)
        {
            Debug.LogError("Mesh is null, cannot save.");
            return;
        }

#if UNITY_EDITOR
        SaveMeshInEditor(mesh, defaultName);
#else
        SaveMeshAtRuntime(mesh, defaultName);
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Saves the mesh as an asset inside the Unity Editor.
    /// </summary>
    private static void SaveMeshInEditor(Mesh mesh, string defaultName)
    {
        // Open save file dialog
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Mesh",
            defaultName,
            "asset",
            "Choose a location to save the mesh"
        );

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("Mesh save canceled.");
            return;
        }

        // Ensure unique asset path
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        // Save the mesh as an asset
        Mesh newMesh = Object.Instantiate(mesh);
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Mesh saved as an asset at: {path}");
    }

    /// <summary>
    /// Adds a Unity Editor Menu Item to save selected mesh.
    /// </summary>
    [MenuItem("Tools/Save Simplified Mesh")]
    public static void SaveSelectedMesh()
    {
        // Get the selected GameObject
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            Debug.LogError("No GameObject selected. Please select a GameObject with a MeshFilter.");
            return;
        }

        MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("Selected GameObject does not have a MeshFilter with a valid mesh.");
            return;
        }

        // Save the mesh
        SaveMeshInEditor(meshFilter.sharedMesh, selectedObject.name + "_Simplified");
    }
#endif

    /// <summary>
    /// Saves the mesh as an OBJ file during runtime (in a built game).
    /// </summary>
    private static void SaveMeshAtRuntime(Mesh mesh, string defaultName)
    {
        string path = Path.Combine(Application.persistentDataPath, defaultName + ".obj");
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("# Exported Mesh");

            // Write vertices
            foreach (Vector3 v in mesh.vertices)
            {
                writer.WriteLine($"v {v.x} {v.y} {v.z}");
            }

            // Write faces
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                writer.WriteLine($"f {triangles[i] + 1} {triangles[i + 1] + 1} {triangles[i + 2] + 1}");
            }
        }

        Debug.Log($"Mesh exported to: {path}");
    }
}



