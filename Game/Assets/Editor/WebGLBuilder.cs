using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds RocketQuiz for the web. Used locally (Tools menu / CLI eval) and by the GitHub Actions
/// workflow (game-ci passes -customBuildPath). Settings target GitHub Pages, which cannot send
/// Content-Encoding headers, so compressed files are decompressed by the loader in JavaScript.
/// </summary>
public static class WebGLBuilder
{
    private const string DefaultOutput = "Builds/WebGL";
    private static readonly string[] Scenes = { "Assets/Scenes/RocketQuiz.unity" };

    [MenuItem("Tools/Build RocketQuiz WebGL")]
    private static void BuildFromMenu()
    {
        Debug.Log(Build());
    }

    public static string Build()
    {
        ApplySettings();

        string output = GetArgument("-customBuildPath") ?? DefaultOutput;
        var options = new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        string result = $"WebGL build {summary.result}: {output}, {summary.totalSize / (1024 * 1024)} MB, " +
                        $"{summary.totalErrors} errors, {summary.totalTime.TotalSeconds:F0} s";

        if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
        {
            throw new Exception(result);
        }

        return result;
    }

    private static void ApplySettings()
    {
        PlayerSettings.productName = "RocketQuiz";
        PlayerSettings.WebGL.template = "PROJECT:RocketQuiz";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
    }

    private static string GetArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
