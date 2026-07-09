using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Day 5: oyuncu prefablari, VFX PF_ isimlendirme, top materyal uyumu.
// Menu: Yakantop > Day 5 Prefab Cleanup
// Batch: -executeMethod Day5PrefabCleanup.Run
// Auto: Day5PrefabCleanup.pending dosyasi varsa domain reload sonrasi bir kez calisir.
public static class Day5PrefabCleanup
{
    const string ScenePath = "Assets/Scenes/Yakantop.unity";
    const string PrefabFolder = "Assets/Prefabs";
    const string MaterialFolder = "Assets/Materials";
    const string PendingFlag = "Assets/Editor/Day5PrefabCleanup.pending";

    [DidReloadScripts]
    static void OnScriptsReloaded()
    {
        if (File.Exists(PendingFlag))
        {
            EditorApplication.delayCall += TryAutoRun;
        }
    }

    static void TryAutoRun()
    {
        if (!File.Exists(PendingFlag))
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorApplication.delayCall += TryAutoRun;
            return;
        }

        RunPendingOnce();
    }

    static void RunPendingOnce()
    {
        if (!File.Exists(PendingFlag))
        {
            return;
        }

        try
        {
            RunInternal();
            File.Delete(PendingFlag);
            string meta = PendingFlag + ".meta";
            if (File.Exists(meta))
            {
                File.Delete(meta);
            }

            Debug.Log("[Day5PrefabCleanup] Auto-run completed (pending flag cleared).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Day5PrefabCleanup] Auto-run failed: " + ex);
        }
    }

    public static void Run()
    {
        try
        {
            RunInternal();
            Debug.Log("[Day5PrefabCleanup] Completed successfully.");
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Day5PrefabCleanup] Failed: " + ex);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Yakantop/Day 5 Prefab Cleanup")]
    public static void RunFromMenu()
    {
        RunInternal();
        Debug.Log("[Day5PrefabCleanup] Completed from menu.");
    }

    static void RunInternal()
    {
        EnsureFolders();
        RenameVfxPrefabs();
        AlignBallPrefabs();
        CreatePlayerPrefabsFromScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
    }

    static void RenameVfxPrefabs()
    {
        RenameAssetIfNeeded(PrefabFolder + "/HitEffect.prefab", PrefabFolder + "/PF_HitEffect.prefab", "PF_HitEffect");
        RenameAssetIfNeeded(PrefabFolder + "/ImpactEffect.prefab", PrefabFolder + "/PF_ImpactEffect.prefab", "PF_ImpactEffect");
        RenameAssetIfNeeded(PrefabFolder + "/Ball_Normal.prefab", PrefabFolder + "/PF_Ball_Normal.prefab", "PF_Ball_Normal");
        RenameAssetIfNeeded(PrefabFolder + "/Ball_Fast.prefab", PrefabFolder + "/PF_Ball_Fast.prefab", "PF_Ball_Fast");
        RenameAssetIfNeeded(PrefabFolder + "/Ball_Heavy.prefab", PrefabFolder + "/PF_Ball_Heavy.prefab", "PF_Ball_Heavy");
        RenameAssetIfNeeded(PrefabFolder + "/Ball_Bouncy.prefab", PrefabFolder + "/PF_Ball_Bouncy.prefab", "PF_Ball_Bouncy");
        RenameAssetIfNeeded(PrefabFolder + "/Ball_Curve.prefab", PrefabFolder + "/PF_Ball_Curve.prefab", "PF_Ball_Curve");
    }

    static void RenameAssetIfNeeded(string fromPath, string toPath, string rootName)
    {
        string already = AssetDatabase.AssetPathToGUID(toPath);
        if (!string.IsNullOrEmpty(already))
        {
            SetPrefabRootName(toPath, rootName);
            return;
        }

        string existing = AssetDatabase.AssetPathToGUID(fromPath);
        if (string.IsNullOrEmpty(existing))
        {
            return;
        }

        string error = AssetDatabase.MoveAsset(fromPath, toPath);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogError("[Day5PrefabCleanup] Move failed " + fromPath + " -> " + toPath + ": " + error);
            return;
        }

        SetPrefabRootName(toPath, rootName);
        Debug.Log("[Day5PrefabCleanup] Renamed " + fromPath + " -> " + toPath);
    }

    static void SetPrefabRootName(string prefabPath, string rootName)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.name != rootName)
            {
                root.name = rootName;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void AlignBallPrefabs()
    {
        Material matNormal = EnsureBallMaterial("Mat_BallNormal", new Color(0.92f, 0.92f, 0.95f));
        Material matFast = EnsureBallMaterial("Mat_BallFast", new Color(1f, 0.85f, 0.2f));
        Material matHeavy = EnsureBallMaterial("Mat_BallHeavy", new Color(0.45f, 0.2f, 0.75f));
        Material matBouncy = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_BallBouncy.mat");
        Material matCurve = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/Mat_BallCurve.mat");

        GameObject impact = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_ImpactEffect.prefab");

        AlignOneBall(PrefabFolder + "/PF_Ball_Normal.prefab", matNormal, impact);
        AlignOneBall(PrefabFolder + "/PF_Ball_Fast.prefab", matFast, impact);
        AlignOneBall(PrefabFolder + "/PF_Ball_Heavy.prefab", matHeavy, impact);
        AlignOneBall(PrefabFolder + "/PF_Ball_Bouncy.prefab", matBouncy, impact);
        AlignOneBall(PrefabFolder + "/PF_Ball_Curve.prefab", matCurve, impact);
    }

    static Material EnsureBallMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        mat.name = name;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void AlignOneBall(string prefabPath, Material material, GameObject hitEffect)
    {
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(prefabPath)))
        {
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            Ball ball = root.GetComponent<Ball>();
            if (ball != null && hitEffect != null)
            {
                ball.hitEffectPrefab = hitEffect;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void CreatePlayerPrefabsFromScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        CreateOrReconnect("RunnerPlayer", PrefabFolder + "/PF_Runner.prefab");
        CreateOrReconnect("SaverPlayer", PrefabFolder + "/PF_Saver.prefab");
        CreateOrReconnect("ThrowerPlayer", PrefabFolder + "/PF_Thrower.prefab");
        CreateOrReconnect("RunnerBot", PrefabFolder + "/PF_RunnerBot.prefab");

        // Instance isimlerini sahne okunurlugu icin koru.
        RenameIfExists("PF_Runner", "RunnerPlayer");
        RenameIfExists("PF_Saver", "SaverPlayer");
        RenameIfExists("PF_Thrower", "ThrowerPlayer");
        RenameIfExists("PF_RunnerBot", "RunnerBot");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void RenameIfExists(string currentName, string desiredName)
    {
        GameObject go = FindInOpenScenes(currentName);
        if (go != null)
        {
            go.name = desiredName;
        }
    }

    static void CreateOrReconnect(string objectName, string prefabPath)
    {
        GameObject sceneObject = FindInOpenScenes(objectName);
        if (sceneObject == null)
        {
            string prefabRootName = Path.GetFileNameWithoutExtension(prefabPath);
            sceneObject = FindInOpenScenes(prefabRootName);
        }

        if (sceneObject == null)
        {
            Debug.LogError("[Day5PrefabCleanup] Scene object not found: " + objectName);
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(sceneObject))
        {
            PrefabUtility.ApplyPrefabInstance(sceneObject, InteractionMode.AutomatedAction);
            Debug.Log("[Day5PrefabCleanup] Applied overrides: " + objectName);
            return;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabPath, InteractionMode.AutomatedAction);
        Debug.Log("[Day5PrefabCleanup] Connected " + objectName + " -> " + prefabPath);
    }

    static GameObject FindInOpenScenes(string objectName)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform found = FindRecursive(roots[i].transform, objectName);
                if (found != null)
                {
                    return found.gameObject;
                }
            }
        }

        return null;
    }

    static Transform FindRecursive(Transform parent, string objectName)
    {
        if (parent.name == objectName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindRecursive(parent.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
