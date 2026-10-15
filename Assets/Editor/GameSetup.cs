using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu  Tools > Generer le jeu  : crée les 2 scènes et les ajoute au Build Settings.
public static class GameSetup
{
    [MenuItem("Tools/Generer le jeu")]
    public static void Generate()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        System.IO.Directory.CreateDirectory("Assets/Scenes");

        CreateScene("MainMenu", "MenuController", typeof(MenuController));
        CreateScene("Level1", "GameManager", typeof(GameManager));

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Level1.unity", true)
        };

        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorUtility.DisplayDialog("Jeu généré",
            "Les scènes MainMenu et Level1 ont été créées.\nOuvrez MainMenu et appuyez sur Play !", "OK");
    }

    static void CreateScene(string sceneName, string objName, System.Type scriptType)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var go = new GameObject(objName);
        go.AddComponent(scriptType);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/" + sceneName + ".unity");
    }
}
