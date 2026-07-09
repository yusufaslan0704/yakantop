using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Tools/Windows Smoke Build
// Batch: -executeMethod WindowsBuildSmoke.Build
public static class WindowsBuildSmoke
{
    const string OutputDir = "Builds/WindowsSmoke";
    const string ExeName = "Yakantop.exe";

    [MenuItem("Tools/Windows Smoke Build")]
    public static void BuildFromMenu()
    {
        BuildInternal(exitOnFinish: false);
    }

    public static void Build()
    {
        BuildInternal(exitOnFinish: true);
    }

    static void BuildInternal(bool exitOnFinish)
    {
        try
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outDir = Path.Combine(projectRoot, OutputDir);
            Directory.CreateDirectory(outDir);

            string exePath = Path.Combine(outDir, ExeName);

            string[] scenes = GetEnabledScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[WindowsBuildSmoke] No enabled scenes in Build Settings.");
                if (exitOnFinish)
                {
                    EditorApplication.Exit(2);
                }

                return;
            }

            Debug.Log("[WindowsBuildSmoke] Building scenes: " + string.Join(", ", scenes));
            Debug.Log("[WindowsBuildSmoke] Output: " + exePath);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            Debug.Log("[WindowsBuildSmoke] Result=" + summary.result +
                      " errors=" + summary.totalErrors +
                      " warnings=" + summary.totalWarnings +
                      " size=" + summary.totalSize +
                      " time=" + summary.totalTime);

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                if (exitOnFinish)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            if (!File.Exists(exePath))
            {
                Debug.LogError("[WindowsBuildSmoke] Build reported success but exe missing: " + exePath);
                if (exitOnFinish)
                {
                    EditorApplication.Exit(3);
                }

                return;
            }

            Debug.Log("[WindowsBuildSmoke] SUCCESS: " + exePath);

            if (exitOnFinish)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[WindowsBuildSmoke] Exception: " + ex);
            if (exitOnFinish)
            {
                EditorApplication.Exit(1);
            }
        }
    }

    static string[] GetEnabledScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled && !string.IsNullOrEmpty(scene.path))
            {
                list.Add(scene.path);
            }
        }

        return list.ToArray();
    }
}
