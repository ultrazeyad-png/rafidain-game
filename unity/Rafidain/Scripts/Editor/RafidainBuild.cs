using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Rafidain.EditorTools
{
    /// <summary>
    /// Menu: Rafidain > Build Android APK / Build Web
    /// Sets the player settings (Next Level, Rafidain, com.nextlevel.rafidain, landscape) and builds every
    /// governorate scene that is in the build list (Zakho first). Output goes to the project's Builds folder.
    /// </summary>
    public static class RafidainBuild
    {
        const string Version = "0.9";

        static void CommonSettings()
        {
            PlayerSettings.companyName = "Next Level";
            PlayerSettings.productName = "Rafidain";
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        static string[] Scenes()
        {
            var list = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (list.Length == 0)
                EditorUtility.DisplayDialog("Rafidain", "No scenes in the build list.\n\nRun Rafidain > Build ALL Governorates first.", "OK");
            return list;
        }

        [MenuItem("Rafidain/Build Android APK")]
        public static void BuildAndroid()
        {
            var scenes = Scenes();
            if (scenes.Length == 0) return;
            CommonSettings();
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.nextlevel.rafidain");
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 9;
            EditorUserBuildSettings.buildAppBundle = false;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            string path = "Builds/Android/Rafidain_v" + Version + ".apk";
            Run(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.Android, options = BuildOptions.None }, path);
        }

        [MenuItem("Rafidain/Build Web (for the Next Level site)")]
        public static void BuildWeb()
        {
            var scenes = Scenes();
            if (scenes.Length == 0) return;
            CommonSettings();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;   // works on any host without special headers

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            string path = "Builds/Web";
            Run(new BuildPlayerOptions { scenes = scenes, locationPathName = path, target = BuildTarget.WebGL, options = BuildOptions.None }, path);
        }

        static void Run(BuildPlayerOptions options, string path)
        {
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                float mb = summary.totalSize / (1024f * 1024f);
                EditorUtility.DisplayDialog("Rafidain", $"Build finished ({mb:0} MB).\n\n{path}", "Show");
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Rafidain", $"Build failed ({summary.totalErrors} errors).\n\nOpen the Console, take a screenshot of the red lines and send it.", "OK");
            }
        }
    }
}
