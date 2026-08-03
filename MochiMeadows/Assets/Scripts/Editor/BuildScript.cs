using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MochiMeadows.EditorTools
{
    // Cross-platform build entry points (callable from batchmode / CLI).
    public static class BuildScript
    {
        const string MacPath = "Builds/Mac/MochiMeadows.app";
        const string WebGLPath = "Builds/WebGL";
        const string WindowsPath = "Builds/Windows/MochiMeadows.exe";
        const string LinuxPath = "Builds/Linux/MochiMeadows.x86_64";

        [MenuItem("Tools/Mochi Meadows/Build macOS")]
        public static void BuildMac() => Build(MacPath, BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone);

        [MenuItem("Tools/Mochi Meadows/Build WebGL")]
        public static void BuildWebGL() => Build(WebGLPath, BuildTarget.WebGL, BuildTargetGroup.WebGL);

        [MenuItem("Tools/Mochi Meadows/Build iOS (Xcode project)")]
        public static void BuildiOS() => Build("Builds/iOS", BuildTarget.iOS, BuildTargetGroup.iOS);

        // `-simulator` builds the Xcode project for the iOS Simulator SDK.
        public static void BuildiOSSim()
        {
            var args = System.Environment.GetCommandLineArgs();
            bool sim = false;
            foreach (var a in args) if (a == "-simulator") sim = true;
            if (sim)
            {
                PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
                PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1); // 1 = ARM64
            }
            Build("Builds/iOS", BuildTarget.iOS, BuildTargetGroup.iOS);
        }

        [MenuItem("Tools/Mochi Meadows/Build Windows")]
        public static void BuildWindows() => Build(WindowsPath, BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone);

        [MenuItem("Tools/Mochi Meadows/Build Linux")]
        public static void BuildLinux() => Build(LinuxPath, BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone);

        static void Build(string path, BuildTarget target, BuildTargetGroup group)
        {
            SceneBuilder.Build();
            ConfigurePlayer(target, group);

            EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { SceneBuilder.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Build failed ({target}): {report.summary.result}\n{string.Join("\n", Array.ConvertAll(report.steps, s => s.ToString()))}");
            Debug.Log($"Build OK -> {path} ({report.summary.totalSize / 1024} KB)");
        }

        static void ConfigurePlayer(BuildTarget target, BuildTargetGroup group)
        {
            PlayerSettings.productName = "Mochi Meadows";
            PlayerSettings.companyName = "Mochi Meadows";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            // IL2CPP is required by WebGL; standalone builds use Mono
            // (works without an activated Pro/Plus IL2CPP entitlement).
            PlayerSettings.SetScriptingBackend(group,
                target == BuildTarget.WebGL ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

            if (target == BuildTarget.WebGL)
            {
                PlayerSettings.WebGL.memorySize = 256;
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            }
        }
    }
}
