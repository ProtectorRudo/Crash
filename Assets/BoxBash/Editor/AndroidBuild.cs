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
        private const string PhoneTestOutput = "Builds/BoxBash-Gameplay.apk";
        private const string DevelopmentOutput = "Builds/BoxBash-Development.apk";

        [MenuItem("BOX BASH/Build Android APK (Phone Test)")]
        public static void BuildPhoneTestApk() => BuildApk(PhoneTestOutput, BuildOptions.None);

        [MenuItem("BOX BASH/Build Android APK (Development)")]
        public static void BuildDevelopmentApk() => BuildApk(DevelopmentOutput, BuildOptions.Development);

        public static void BuildPhoneTestApkBatch() => BuildPhoneTestApk();
        public static void BuildDevelopmentApkBatch() => BuildDevelopmentApk();

        [MenuItem("BOX BASH/Build Android APK")]
        public static void BuildLegacyApk() => BuildDevelopmentApk();

        private static void BuildApk(string output, BuildOptions options)
        {
            if (!File.Exists(Scene))
                throw new FileNotFoundException("No se encontró la escena principal.", Scene);

            Directory.CreateDirectory("Builds");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            ConfigurePlayerSettings();

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                    throw new System.Exception("Unity no pudo cambiar la plataforma activa a Android. Verificá Android Build Support + SDK/NDK + OpenJDK en Unity Hub.");
            }

            EditorUserBuildSettings.buildAppBundle = false;

            BuildPlayerOptions build = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = output,
                target = BuildTarget.Android,
                options = options
            };

            BuildReport report = BuildPipeline.BuildPlayer(build);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Build Android falló: {summary.result} ({summary.totalErrors} errores)");

            string mode = (options & BuildOptions.Development) != 0 ? "DEVELOPMENT" : "PHONE TEST";
            Debug.Log($"BOX BASH APK {mode} READY: {Path.GetFullPath(output)} | {summary.totalSize / (1024f * 1024f):0.0} MB | {summary.totalTime}");
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "ProtectorRudo";
            PlayerSettings.productName = "Box Bash";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.protectorrudo.boxbash");
            PlayerSettings.bundleVersion = "0.7.1";
            PlayerSettings.Android.bundleVersionCode = 8;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // First-device truth gate: mirror the PogoDom approach and let the Unity Hub
            // Android toolchain choose its supported backend/architectures. We can harden
            // release architecture/backend after the APK is running on a real phone.
            EditorUserBuildSettings.buildAppBundle = false;
        }
    }
}
#endif
