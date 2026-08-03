using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MochiMeadows.Core;

namespace MochiMeadows.EditorTools
{
    // Creates the (deliberately tiny) game scene: one Boot object.
    // Everything else is built at runtime by GameBootstrap.
    public static class SceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MochiMeadows.unity";

        [MenuItem("Tools/Mochi Meadows/Build Scene")]
        public static void Build()
        {
            if (File.Exists(ScenePath))
            {
                EnsureInBuildSettings();
                return;
            }
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootGo = new GameObject("Boot");
            bootGo.AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureInBuildSettings();
            Debug.Log($"Mochi Meadows scene created at {ScenePath}");
        }

        static void EnsureInBuildSettings()
        {
            var scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            EditorBuildSettings.scenes = scenes;
        }
    }
}
