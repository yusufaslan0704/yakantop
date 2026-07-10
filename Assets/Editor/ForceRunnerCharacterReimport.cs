using UnityEditor;
using UnityEngine;

// Menu: Tools/Force Reimport Role Characters
public static class ForceRunnerCharacterReimport
{
    static readonly string[] ModelPaths =
    {
        "Assets/Resources/Models/RunnerCharacter.fbx",
        "Assets/Resources/Models/SaverCharacter.fbx",
        "Assets/Resources/Models/ThrowerCharacter.fbx"
    };

    [MenuItem("Tools/Force Reimport Role Characters")]
    public static void Run()
    {
        foreach (string modelPath in ModelPaths)
        {
            Reimport(modelPath);
        }
    }

    public static void Reimport(string modelPath)
    {
        ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;

        if (importer == null)
        {
            Debug.LogWarning("Model bulunamadi: " + modelPath);
            return;
        }

        // Import scale 1: cift olceklemeyi onler. Boyutu prefab modelScale + auto-fit verir.
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        // Avatar map'i sifirla; CreateFromThisModel yeniden uretsin.
        HumanDescription human = importer.humanDescription;
        human.human = System.Array.Empty<HumanBone>();
        human.skeleton = System.Array.Empty<SkeletonBone>();
        importer.humanDescription = human;

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        float hipY = -1f;

        if (prefab != null)
        {
            Transform hips = FindHips(prefab.transform);
            if (hips != null)
            {
                hipY = hips.localPosition.y;
            }
        }

        Debug.Log(modelPath + " reimport OK. globalScale=" + importer.globalScale + " hipY=" + hipY.ToString("F4"));
    }

    static Transform FindHips(Transform root)
    {
        if (root.name.Contains("Hips"))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindHips(root.GetChild(i));
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
