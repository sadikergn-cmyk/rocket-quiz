using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds RocketQuiz for the web. Used locally (Tools menu / CLI eval) and by the GitHub Actions
/// workflow (game-ci passes -customBuildPath). Settings target GitHub Pages, which cannot send
/// Content-Encoding headers, so compressed files are decompressed by the loader in JavaScript.
/// A successful build also writes rq-build.json next to Build/: the file list the Nadide Next.js host loads the game
/// from (tools/package_webgl.ps1 zips both for a GitHub Release).
/// </summary>
public static class WebGLBuilder
{
    public const string ManifestName = "rq-build.json";

    private const string DefaultOutput = "Builds/WebGL";
    private static readonly string[] Scenes = { "Assets/Scenes/RocketQuiz.unity" };

    // Build/ file names (nameFilesAsHashes: "<hash>.<kind>[.gz|.br|.unityweb]").
    private static readonly Regex LoaderFile = new Regex(@"\.loader\.js$");
    private static readonly Regex DataFile = new Regex(@"\.data(\.(gz|br|unityweb))?$");
    private static readonly Regex FrameworkFile = new Regex(@"\.framework\.js(\.(gz|br|unityweb))?$");
    private static readonly Regex CodeFile = new Regex(@"\.wasm(\.(gz|br|unityweb))?$");

    // rq-build.json; field names are the host's contract.
    [Serializable]
    public class BuildManifest
    {
        public string version;
        public string loaderUrl;
        public string dataUrl;
        public string frameworkUrl;
        public string codeUrl;
        public string companyName;
        public string productName;
        public string productVersion;
    }

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

        if (summary.result == BuildResult.Succeeded)
        {
            // Never fails the build (the Pages workflow does not need the manifest); the packaging script checks it.
            try
            {
                BuildManifest manifest = WriteManifest(output, GetArgument("-rqVersion"));
                result += $", {ManifestName} {manifest.version}";
            }
            catch (Exception e)
            {
                Debug.LogError($"{ManifestName} not written: {e.Message}");
                result += $", {ManifestName} MISSING ({e.Message})";
            }
        }

        return result;
    }

    // Writes <output>/rq-build.json from the files in <output>/Build. `version` defaults to
    // v<bundleVersion>-<UTC yyyyMMddHHmm>, so every build gets its own release tag.
    public static BuildManifest WriteManifest(string output, string version)
    {
        string buildDir = Path.Combine(output, "Build");
        string[] files = Directory.Exists(buildDir) ? Directory.GetFiles(buildDir) : new string[0];
        for (int i = 0; i < files.Length; i++) files[i] = Path.GetFileName(files[i]);

        BuildManifest manifest = CreateManifest(files,
            string.IsNullOrEmpty(version) ? "v" + PlayerSettings.bundleVersion + "-" + DateTime.UtcNow.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture) : version,
            PlayerSettings.companyName, PlayerSettings.productName, PlayerSettings.bundleVersion);
        File.WriteAllText(Path.Combine(output, ManifestName), JsonUtility.ToJson(manifest, true));
        return manifest;
    }

    // Build/ file names -> manifest with paths relative to the build root ("Build/<name>").
    public static BuildManifest CreateManifest(string[] buildFiles, string version, string companyName, string productName, string productVersion)
    {
        return new BuildManifest
        {
            version = version,
            loaderUrl = Find(buildFiles, LoaderFile, "loader"),
            dataUrl = Find(buildFiles, DataFile, "data"),
            frameworkUrl = Find(buildFiles, FrameworkFile, "framework"),
            codeUrl = Find(buildFiles, CodeFile, "wasm"),
            companyName = companyName,
            productName = productName,
            productVersion = productVersion,
        };
    }

    private static string Find(string[] files, Regex pattern, string kind)
    {
        string found = null;
        foreach (string name in files)
        {
            if (!pattern.IsMatch(name)) continue;
            if (found != null) throw new InvalidOperationException($"WebGL build has two {kind} files: {found}, {name}");
            found = name;
        }
        if (found == null) throw new InvalidOperationException($"WebGL build has no {kind} file in Build/");
        return "Build/" + found;
    }

    private static void ApplySettings()
    {
        PlayerSettings.productName = "RocketQuiz";
        PlayerSettings.WebGL.template = "PROJECT:RocketQuiz";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        // Stays on until Vercel's Content-Encoding headers are verified (then the host serves .gz directly).
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
