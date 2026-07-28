using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AlgoLabCommandLineBuild
{
    public static void BuildAndroid()
    {
        var outputPath = Environment.GetEnvironmentVariable("ALGOLAB_ANDROID_APK");
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            outputPath = Path.Combine("Builds", "Android", "algolab.apk");
        }

        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

        var scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled && File.Exists(scene.path))
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            throw new InvalidOperationException("No hay escenas habilitadas para compilar.");
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            targetGroup = BuildTargetGroup.Android,
            target = BuildTarget.Android,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log(
            $"ALGOLAB_ANDROID_BUILD result={summary.result} " +
            $"errors={summary.totalErrors} warnings={summary.totalWarnings} " +
            $"size={summary.totalSize} output={outputPath}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"La compilación Android falló: {summary.result}, errores={summary.totalErrors}.");
        }
    }
}
