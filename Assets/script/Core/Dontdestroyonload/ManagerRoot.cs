using UnityEngine;

public class ManagerRootBootstrap : MonoBehaviour
{
    public static ManagerRootBootstrap Instance;

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