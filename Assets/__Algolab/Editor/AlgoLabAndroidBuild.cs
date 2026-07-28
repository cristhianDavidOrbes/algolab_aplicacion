#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AlgoLabAndroidBuild
{
    public const string OutputPath =
        "Builds/Android/algolab.apk";

    public static void BuildLevel3Practice()
    {
        AlgoLabLevel4AbstractionSetup.ConfigureBatch();

        EditorBuildSettingsScene[] activas = Array.FindAll(
            EditorBuildSettings.scenes,
            scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path)
        );
        string[] scenes = Array.ConvertAll(activas, scene => scene.path);

        if (scenes.Length == 0)
            throw new InvalidOperationException("No hay escenas activas para compilar.");

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        EditorUserBuildSettings.buildAppBundle = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.CleanBuildCache
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        string resultado =
            "Resultado=" + summary.result + Environment.NewLine +
            "APK=" + Path.GetFullPath(OutputPath) + Environment.NewLine +
            "Tamano=" + summary.totalSize + Environment.NewLine +
            "Tiempo=" + summary.totalTime + Environment.NewLine +
            "Errores=" + summary.totalErrors + Environment.NewLine +
            "Advertencias=" + summary.totalWarnings + Environment.NewLine;
        File.WriteAllText("Logs/level3-android-build-result.txt", resultado);

        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                "Falló la compilación Android: " + summary.result
            );

        Debug.Log("ALGOLAB_ANDROID_BUILD_OK: " + OutputPath);
    }
}
#endif
