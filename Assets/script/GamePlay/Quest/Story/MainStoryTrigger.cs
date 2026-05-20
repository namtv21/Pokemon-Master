using UnityEngine;

public class MainStoryTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string triggerId;
    [SerializeField] private bool triggerOnPlayerEnter = true;
    [SerializeField] private bool triggerOnSceneStart;
    [SerializeField] private bool restrictToScene;
    [SerializeField] private string requiredSceneName;
    [SerializeField] private bool oneShot = true;

    private bool hasTriggered;

    private void Start()
    {
        if (triggerOnSceneStart)
            TryTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnPlayerEnter)
            return;

        if (!other.CompareTag("Player"))
            return;

        TryTrigger();
    }

    public void TryTrigger()
    {
        Debug.Log($"[MainStoryTrigger] TryTrigger called (id='{triggerId}', oneShot={oneShot}, hasTriggered={hasTriggered}, restrictToScene={restrictToScene}, requiredScene='{requiredSceneName}')");

        if (oneShot && hasTriggered)
        {
            Debug.Log("[MainStoryTrigger] Skipping because already triggered (oneShot)");
            return;
        }

        if (restrictToScene && !string.IsNullOrWhiteSpace(requiredSceneName))
        {
            var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[MainStoryTrigger] Active scene='{activeSceneName}', required='{requiredSceneName}'");
            if (!string.Equals(activeSceneName, requiredSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("[MainStoryTrigger] Scene restriction prevents trigger");
                return;
            }
        }

        var director = MainStoryDirector.Instance;
        if (director == null)
        {
            Debug.LogWarning("[MainStoryTrigger] No MainStoryDirector instance found");
            return;
        }

        var result = director.TryTrigger(triggerId);
        if (result)
        {
            hasTriggered = true;
            
            // Deactivate visual marker when trigger activated (tìm trên con cũng được)
            var triggerVisual = GetComponentInChildren<StoryTriggerVisual>();
            if (triggerVisual != null)
            {
                triggerVisual.Deactivate();
            }
        }
    }
}