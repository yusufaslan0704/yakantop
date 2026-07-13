using System.IO;
using UnityEditor;
using UnityEngine;

// Menu: Tools/Windows Smoke Build
// Batch: -executeMethod WindowsBuildSmoke.Build
// File trigger: Temp/RequestWindowsSmokeBuild (Editor acikken)
public static class WindowsBuildSmoke
{
    const string OutputDir = "Builds/WindowsSmoke";
    const string ExeName = "Yakantop.exe";
    const string RequestPath = "Temp/RequestWindowsSmokeBuild";
    const string StatusPath = "Temp/WindowsSmokeBuildStatus.txt";

    static bool building;
    static double resumeAt = -1;
    static bool hookRegistered;

    [InitializeOnLoadMethod]
    static void RegisterFileTrigger()
    {
        if (hookRegistered)
        {
            return;
        }

        hookRegistered = true;
        EditorApplication.update += TickFileTrigger;
    }

    static void TickFileTrigger()
    {
        if (building)
        {
            return;
        }

        if (resumeAt > 0 && EditorApplication.timeSinceStartup >= resumeAt)
        {
            resumeAt = -1;
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RunTriggeredBuild();
            }

            return;
        }

        if (!File.Exists(RequestPath))
        {
            return;
        }

        try
        {
            File.Delete(RequestPath);
        }
        catch
        {
            return;
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WriteStatus("stopping-playmode");
            Debug.Log("[WindowsBuildSmoke] Exiting Play Mode for smoke build...");
            EditorApplication.isPlaying = false;
            resumeAt = EditorApplication.timeSinceStartup + 1.5;
            return;
        }

        RunTriggeredBuild();
    }

    static void RunTriggeredBuild()
    {
        WriteStatus("building");
        Debug.Log("[WindowsBuildSmoke] File trigger → starting smoke build.");
        BuildFromMenu();
        WriteStatus("done");
    }

    static void WriteStatus(string text)
    {
        try
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(StatusPath, text + "\n" + System.DateTime.Now.ToString("o"));
        }
        catch
        {
            // ignore
        }
    }

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
        building = true;
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
                WriteStatus("failed");
                if (exitOnFinish)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            if (!File.Exists(exePath))
            {
                Debug.LogError("[WindowsBuildSmoke] Build reported success but exe missing: " + exePath);
                WriteStatus("missing-exe");
                if (exitOnFinish)
                {
                    EditorApplication.Exit(3);
                }

                return;
            }

            Debug.Log("[WindowsBuildSmoke] SUCCESS: " + exePath);
            WriteStatus("done");

            if (exitOnFinish)
            {
                EditorApplication.Exit(0);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[WindowsBuildSmoke] Exception: " + ex);
            WriteStatus("error: " + ex.Message);
            if (exitOnFinish)
            {
                EditorApplication.Exit(1);
            }
        }
        finally
        {
            building = false;
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
