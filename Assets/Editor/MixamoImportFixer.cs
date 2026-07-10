using System.IO;
using UnityEditor;
using UnityEngine;

// Resources/Models altindaki Mixamo FBX'leri icin import ayarlarini otomatik duzeltir:
// - Rig'i Humanoid'e cevirir (klipler modeller arasinda sorunsuz calisir).
// - Klipleri gercek "loop" yapar (Loop Time + Loop Pose) -> dongu dikissiz olur.
// - Kok hareketinin XZ kismini pozdan ayiklar -> "In Place" olmayan kosu
//   animasyonu karakteri ileri kaydirmaz (fizik zaten hareketi sagliyor).
public class MixamoImportFixer : AssetPostprocessor
{
    private const string ModelsFolder = "Assets/Resources/Models";

    // Bu sayiyi artirmak tum modellerin yeniden import edilmesini tetikler.
    public override uint GetVersion()
    {
        return 11;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelsFolder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;

        // Runner/Saver/ThrowerCharacter: cm olcekli Mixamo FBX.
        // Import scale 1 birak; boyutu runtime modelScale + auto-fit ayarlar.
        // (globalScale 250 + humanDescription.globalScale 250 cift olcekleyip
        //  kemikleri kilometrelerce sisiriyordu.)
        if (assetPath.Contains("RunnerCharacter") ||
            assetPath.Contains("SaverCharacter") ||
            assetPath.Contains("ThrowerCharacter"))
        {
            if (FbxLooksRigged(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.globalScale = 1f;
                importer.useFileScale = true;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.globalScale = 1f;
                importer.useFileScale = true;
            }

            HumanDescription human = importer.humanDescription;
            human.human = System.Array.Empty<HumanBone>();
            human.skeleton = System.Array.Empty<SkeletonBone>();
            importer.humanDescription = human;

            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
    }

    void OnPostprocessModel(GameObject root)
    {
        // no-op: HumanDescription.globalScale Unity 6 API'de yok.
        // Olcek sadece ModelImporter.globalScale ile yonetilir.
    }

    // FBX binary/ascii icinde kemik/skin izi var mi diye hizli bak.
    static bool FbxLooksRigged(string path)
    {
        try
        {
            byte[] data = File.ReadAllBytes(path);
            if (ContainsAscii(data, "mixamorig") ||
                ContainsAscii(data, "Armature") ||
                ContainsAscii(data, "Cluster") ||
                ContainsAscii(data, "Deformer") ||
                ContainsAscii(data, "LimbNode") ||
                ContainsAscii(data, "SkinnedMesh"))
            {
                return true;
            }
        }
        catch
        {
            // Okunamazsa guvenli taraf: Generic.
        }

        return false;
    }

    static bool ContainsAscii(byte[] data, string token)
    {
        byte[] needle = System.Text.Encoding.ASCII.GetBytes(token);
        int limit = data.Length - needle.Length;

        for (int i = 0; i <= limit; i++)
        {
            bool match = true;

            for (int j = 0; j < needle.Length; j++)
            {
                if (data[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(ModelsFolder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0) return;

        // Tek seferlik aksiyon klipleri dongulenmez.
        string path = assetPath;
        bool oneShot =
            path.Contains("Jump") ||
            path.Contains("Throw") ||
            path.Contains("Hit") ||
            path.Contains("Dash") ||
            path.Contains("Dodge") ||
            path.Contains("Revive") ||
            path.Contains("Fireball");

        bool looping = !oneShot;

        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = looping;
            clip.loopPose = looping;

            // Donme ve yukseklik pozda kalsin (ziplama/inis gorseli bozulmasin),
            // ileri kayma (XZ) ise root motion'a ayrilsin. Animator'da
            // applyRootMotion kapali oldugu icin bu kayma yok sayilir:
            // karakter yerinde kosar, hareketi fizik verir.
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = false;

            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
        }

        importer.clipAnimations = clips;
    }
}

// Role karakterleri henuz import edilmediyse veya Humanoid map bos ise otomatik tetikler.
[InitializeOnLoad]
static class RoleCharacterImportEnsurer
{
    static readonly string[] ModelPaths =
    {
        "Assets/Resources/Models/RunnerCharacter.fbx",
        "Assets/Resources/Models/SaverCharacter.fbx",
        "Assets/Resources/Models/ThrowerCharacter.fbx"
    };

    static RoleCharacterImportEnsurer()
    {
        EditorApplication.delayCall += EnsureImported;
    }

    static void EnsureImported()
    {
        foreach (string modelPath in ModelPaths)
        {
            if (!File.Exists(modelPath))
            {
                continue;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            bool hasMesh = false;
            bool hasAvatar = false;

            if (assets != null)
            {
                foreach (Object asset in assets)
                {
                    if (asset is Mesh)
                    {
                        hasMesh = true;
                    }

                    if (asset is Avatar)
                    {
                        hasAvatar = true;
                    }
                }
            }

            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            bool needsHuman = importer != null &&
                              (importer.animationType != ModelImporterAnimationType.Human ||
                               importer.humanDescription.human == null ||
                               importer.humanDescription.human.Length == 0);

            bool hipsExploded = false;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab != null)
            {
                Transform hips = FindNamed(prefab.transform, "Hips");
                if (hips != null)
                {
                    float hipY = Mathf.Abs(hips.localPosition.y);
                    if (hipY > 10f)
                    {
                        hipsExploded = true;
                    }
                }
            }

            if (!hasMesh || !hasAvatar || needsHuman || hipsExploded)
            {
                Debug.Log(modelPath + " force import (mesh=" + hasMesh +
                          ", avatar=" + hasAvatar +
                          ", humanFix=" + needsHuman +
                          ", hipsExploded=" + hipsExploded + ")");

                ForceRunnerCharacterReimport.Reimport(modelPath);
            }
        }
    }

    static Transform FindNamed(Transform root, string token)
    {
        if (root.name.Contains(token))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindNamed(root.GetChild(i), token);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
