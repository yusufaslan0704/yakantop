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
        return 4;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelsFolder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;

        // RunnerCharacter: rig varsa Humanoid (Mixamo klipleri calisir),
        // rig yoksa Generic + animasyon kapali (statik mesh).
        if (assetPath.Contains("RunnerCharacter"))
        {
            if (FbxLooksRigged(assetPath))
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.globalScale = 1f;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                importer.globalScale = 1f;
            }

            return;
        }

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
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

// RunnerCharacter henuz import edilmediyse (Resources.Load null doner) otomatik tetikler.
[InitializeOnLoad]
static class RunnerCharacterImportEnsurer
{
    const string ModelPath = "Assets/Resources/Models/RunnerCharacter.fbx";

    static RunnerCharacterImportEnsurer()
    {
        EditorApplication.delayCall += EnsureImported;
    }

    static void EnsureImported()
    {
        if (!File.Exists(ModelPath))
        {
            return;
        }

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        bool hasMesh = false;

        if (assets != null)
        {
            foreach (Object asset in assets)
            {
                if (asset is Mesh)
                {
                    hasMesh = true;
                    break;
                }
            }
        }

        if (!hasMesh)
        {
            Debug.Log("RunnerCharacter.fbx import ediliyor...");
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
