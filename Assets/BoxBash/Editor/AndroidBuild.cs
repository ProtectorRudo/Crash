#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BoxBash.EditorTools
{
    public static class AndroidBuild
    {
        private const string Scene = "Assets/Scenes/Main.unity";
        private const string Output = "Builds/BoxBash-Gameplay.apk";

        [MenuItem("BOX BASH/Build Android APK")]
        public static void BuildApk()
        {
            if (!File.Exists(Scene))
                throw new FileNotFoundException("No se encontró la escena principal.", Scene);

            Directory.CreateDirectory("Builds");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };

            PlayerSettings.companyName = "BoxBash";
            PlayerSettings.productName = "Box Bash";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.boxbash.spacecrate");
            PlayerSettings.bundleVersion = "0.4.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            EditorUserBuildSettings.buildAppBundle = false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new System.Exception("Unity no pudo cambiar la plataforma activa a Android.");
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Output,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Build Android falló: {summary.result} ({summary.totalErrors} errores)");

            Debug.Log($"BOX BASH APK OK: {Path.GetFullPath(Output)} | {summary.totalSize / (1024f * 1024f):0.0} MB");
            EditorUtility.RevealInFinder(Output);
        }
    }
}
#endif
