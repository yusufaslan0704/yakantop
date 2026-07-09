using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor Untitled/bos sahneyle acilirsa Yakantop'u otomatik acar.
[InitializeOnLoad]
public static class OpenMainSceneOnLoad
{
    const string MainScenePath = "Assets/Scenes/Yakantop.unity";

    static OpenMainSceneOnLoad()
    {
        EditorApplication.delayCall += TryOpenMainScene;
    }

    static void TryOpenMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene active = SceneManager.GetActiveScene();
        bool isEmptyUntitled = string.IsNullOrEmpty(active.path) && active.rootCount == 0;
        bool alreadyMain = active.path == MainScenePath;

        if (alreadyMain || !isEmptyUntitled)
        {
            return;
        }

        if (!System.IO.File.Exists(MainScenePath))
        {
            Debug.LogWarning("[OpenMainSceneOnLoad] Ana sahne bulunamadi: " + MainScenePath);
            return;
        }

        EditorSceneManager.OpenScene(MainScenePath);
        Debug.Log("[OpenMainSceneOnLoad] Acildi: " + MainScenePath);
    }

    [MenuItem("Yakantop/Open Main Scene (Yakantop)")]
    public static void OpenFromMenu()
    {
        EditorSceneManager.OpenScene(MainScenePath);
    }
}
