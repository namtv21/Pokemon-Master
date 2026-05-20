using UnityEngine;
using UnityEngine.SceneManagement;

public static class BootstrapLoader
{
    private static readonly string[] ResourcePaths =
    {
        "SystemRoot",
        "SystemsRoot"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Don't load SystemRoot in Main Menu
        var currentScene = SceneManager.GetActiveScene();
        if (!string.IsNullOrWhiteSpace(currentScene.name) && 
            currentScene.name.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Object.FindObjectOfType<GameController>() != null)
            return;

        GameObject prefab = null;
        string loadedPath = null;

        foreach (var resourcePath in ResourcePaths)
        {
            prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null)
            {
                loadedPath = resourcePath;
                break;
            }
        }

        if (prefab == null)
        {
            Debug.LogError("Missing SystemRoot prefab. Put one of these at Assets/Resources/: SystemRoot.prefab or SystemsRoot.prefab");
            return;
        }

        var instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);
    }
}