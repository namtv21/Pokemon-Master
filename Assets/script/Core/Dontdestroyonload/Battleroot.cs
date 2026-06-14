using UnityEngine;

public class BattleRootBootstrap : MonoBehaviour
{
   public static BattleRootBootstrap Instance;

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