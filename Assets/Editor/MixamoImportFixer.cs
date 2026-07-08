using UnityEditor;

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
        return 1;
    }

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(ModelsFolder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;

        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(ModelsFolder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;

        if (clips == null || clips.Length == 0) return;

        // Zipla tek seferlik oynar, digerleri (idle, kosu, danslar) donguseldir.
        bool looping = !assetPath.Contains("Jump");

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
