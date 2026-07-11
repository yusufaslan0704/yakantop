using UnityEngine;
using UnityEngine.SceneManagement;

// Sahne hierarchy klasorlerine runtime erisim.
public static class SceneFolders
{
    public const string Managers = "Managers";
    public const string Cameras = "Cameras";
    public const string Arena = "Arena";
    public const string Players = "Players";
    public const string UI = "UI";
    public const string RuntimeSpawned = "RuntimeSpawned";
    public const string DisabledTestObjects = "Disabled_TestObjects";

    public static Transform Find(string folderName)
    {
        if (string.IsNullOrEmpty(folderName))
        {
            return null;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == folderName)
            {
                return roots[i].transform;
            }
        }

        return null;
    }

    public static void ParentTo(Transform child, string folderName)
    {
        if (child == null)
        {
            return;
        }

        Transform folder = Find(folderName);
        if (folder != null)
        {
            child.SetParent(folder, true);
        }
    }
}
