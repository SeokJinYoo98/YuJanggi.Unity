using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YuJanggi.Editor
{
    public static class SceneShortcuts
    {
        [MenuItem("YuJanggi/Scenes/Bootstrap &1")]
        private static void OpenBootstrap()
            => OpenScene("Assets/Scenes/BootStrapScene.unity");

        [MenuItem("YuJanggi/Scenes/Login &2")]
        private static void OpenLogin()
            => OpenScene("Assets/Scenes/LoginScene.unity");

        [MenuItem("YuJanggi/Scenes/Lobby &3")]
        private static void OpenLobby()
            => OpenScene("Assets/Scenes/LobbyScene.unity");

        [MenuItem("YuJanggi/Scenes/Janggi &4")]
        private static void OpenJanggi()
            => OpenScene("Assets/Scenes/JanggiScene.unity");

        [MenuItem("YuJanggi/Scenes/Bootstrap &1", true)]
        [MenuItem("YuJanggi/Scenes/Login &2", true)]
        [MenuItem("YuJanggi/Scenes/Lobby &3", true)]
        [MenuItem("YuJanggi/Scenes/Janggi &4", true)]
        private static bool CanOpenScene()
            => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static void OpenScene(string path)
        {
            if (!CanOpenScene()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"Scene을 찾을 수 없습니다: {path}");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
