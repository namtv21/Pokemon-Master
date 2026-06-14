using UnityEngine;

public class UIRootBootstrap : MonoBehaviour
{
    public static UIRootBootstrap Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // huỷ bản mới nếu đã có
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

}