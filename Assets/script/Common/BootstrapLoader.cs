using UnityEngine;
using UnityEngine.SceneManagement;

public static class BootstrapLoader
{
    private static readonly string[] ResourcePaths =
    {
        "SystemRoot",
        "SystemsRoot"
    };

    private static readonly string[] MainMenuSceneNames =
    {
        "MainMenu",
        "MainMenuScreen",
        "Intro"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnFirstGameSceneLoaded;

        var currentScene = SceneManager.GetActiveScene();
        bool isMainMenu = !string.IsNullOrWhiteSpace(currentScene.name) &&
            System.Array.Exists(MainMenuSceneNames, s =>
                currentScene.name.Equals(s, System.StringComparison.OrdinalIgnoreCase));

        if (isMainMenu)
        {
            // MainMenu không cần SystemRoot, nhưng đăng ký để tạo khi scene game đầu tiên load
            SceneManager.sceneLoaded += OnFirstGameSceneLoaded;
            return;
        }

        EnsureSystemRoot();
    }

    private static void OnFirstGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMainMenu = System.Array.Exists(MainMenuSceneNames, s =>
            scene.name.Equals(s, System.StringComparison.OrdinalIgnoreCase));

        if (isMainMenu)
            return; // vẫn còn trong MainMenu, tiếp tục chờ scene game thật sự

        SceneManager.sceneLoaded -= OnFirstGameSceneLoaded;
        EnsureSystemRoot();
    }

    public static GameObject EnsureSystemRoot()
    {
        var existingController = Object.FindObjectOfType<GameController>(true);
        if (existingController != null)
            return existingController.transform.root.gameObject;

        GameObject prefab = null;

        foreach (var resourcePath in ResourcePaths)
        {
            prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
                break;
        }

        if (prefab == null)
        {
            Debug.LogError("Missing SystemRoot prefab. Put one of these at Assets/Resources/: SystemRoot.prefab or SystemsRoot.prefab");
            return null;
        }

        var instance = Object.Instantiate(prefab);
        instance.name = "SystemRoot (Runtime)";
        Object.DontDestroyOnLoad(instance);
        return instance;
    }
}
