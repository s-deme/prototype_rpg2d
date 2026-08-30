using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reproducible Windows build entry point for local use and CI.</summary>
public static class AliceRpgBuild
{
    private const string OutputDirectory = "Builds/Windows";
    private const string OutputFile = "AliceAndTheBrokenCrown.exe";
    private const string ProductVersion = AliceRpgBuildInfo.Version;
    private const string AppIconAsset = "Assets/Branding/AliceAppIcon.png";
    private static readonly string[] DocumentationFiles = { "README.md", "CREDITS.md", "PRIVACY.md", "KNOWN_ISSUES.md", "BUILDING.md" };

    [MenuItem("Alice RPG/Configure Player Settings")]
    public static void ConfigurePlayerSettings()
    {
        PlayerSettings.companyName = "Wonderland Workshop";
        PlayerSettings.productName = "Alice & The Broken Crown";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.usePlayerLog = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.bundleVersion = ProductVersion;
        AssetDatabase.ImportAsset(AppIconAsset, ImportAssetOptions.ForceUpdate);
        Texture2D appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AppIconAsset);
        if (appIcon == null) throw new BuildFailedException("Missing Windows application icon: " + AppIconAsset);
        int[] iconSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
        Texture2D[] icons = new Texture2D[iconSizes.Length];
        for (int i = 0; i < icons.Length; i++) icons[i] = appIcon;
        PlayerSettings.SetIcons(NamedBuildTarget.Standalone, icons, IconKind.Application);
        AssetDatabase.SaveAssets();
        Debug.Log("Alice RPG player settings configured.");
    }

    [MenuItem("Alice RPG/Build Windows")]
    public static void BuildWindows()
    {
        ConfigurePlayerSettings();
        Directory.CreateDirectory(OutputDirectory);
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = Path.Combine(OutputDirectory, OutputFile),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Windows build failed: " + report.summary.result);
        CopyReleaseDocumentation();
        Debug.Log("Build succeeded: " + report.summary.outputPath + " (" + report.summary.totalSize + " bytes)");
    }

    private static void CopyReleaseDocumentation()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string destination = Path.Combine(OutputDirectory, "Documentation");
        Directory.CreateDirectory(destination);
        for (int i = 0; i < DocumentationFiles.Length; i++)
        {
            string source = Path.Combine(projectRoot, DocumentationFiles[i]);
            if (!File.Exists(source)) throw new BuildFailedException("Missing release documentation: " + DocumentationFiles[i]);
            File.Copy(source, Path.Combine(destination, DocumentationFiles[i]), true);
        }
    }
}
