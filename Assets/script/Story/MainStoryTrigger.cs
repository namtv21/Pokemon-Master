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
    private float nextOverlapPollTime;

    private bool ShouldIgnoreSavedTriggeredState()
    {
        if (string.IsNullOrWhiteSpace(triggerId))
            return false;

        var director = MainStoryDirector.Instance;
        if (director == null)
            return false;

        return director.IsCurrentStep(triggerId) || director.CanTrigger(triggerId);
    }

    private void Start()
    {
        var col = GetComponent<Collider2D>();

        // If this is a one-shot trigger, GameController/SaveLoadSystem may apply saved state after scene load.
        // Older PlayerPrefs fallback has been removed to avoid accidentally hiding triggers.
        // If there's pending load data (LoadFromMenu or scene switch), hide instantly based on that to avoid blink
        try
        {
            var pending = SaveLoadSystem.pendingLoadData;
            bool savedAsTriggered = pending != null
                ? pending.triggeredTriggers != null && pending.triggeredTriggers.Exists(
                    id => string.Equals(id, triggerId, System.StringComparison.OrdinalIgnoreCase))
                : SaveLoadSystem.IsRuntimeTriggered(triggerId);
            if (!ShouldIgnoreSavedTriggeredState() && savedAsTriggered)
            {
                hasTriggered = true;
                var triggerVisualExisting2 = GetComponentInChildren<StoryTriggerVisual>();
                if (triggerVisualExisting2 != null)
                    triggerVisualExisting2.ForceHideInstant();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MainStoryTrigger] Could not inspect pending save state for '{triggerId}': {ex.Message}", this);
        }
        if (triggerOnSceneStart)
            StartCoroutine(TryTriggerWhenLoadFinishes());

        if (triggerOnPlayerEnter)
            StartCoroutine(TryTriggerIfPlayerAlreadyInside());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnPlayerEnter)
            return;

        if (!other.CompareTag("Player"))
            return;

        TryTrigger();
    }

    private void Update()
    {
        if (!triggerOnPlayerEnter || (oneShot && hasTriggered))
            return;

        if (Time.time < nextOverlapPollTime)
            return;

        nextOverlapPollTime = Time.time + 0.2f;

        if (!CanTriggerNow())
            return;

        TryTriggerIfPlayerOverlappingNow();
    }

    public void TryTrigger()
    {
        if (SaveLoadSystem.IsLoadInProgress)
            return;

        if (oneShot && hasTriggered)
        {
            return;
        }

        if (restrictToScene && !string.IsNullOrWhiteSpace(requiredSceneName))
        {
            var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.Equals(activeSceneName, requiredSceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var director = MainStoryDirector.Instance;
        if (director == null)
        {
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

            // Persist one-shot state so it stays hidden across scene reloads
            if (oneShot && !string.IsNullOrWhiteSpace(triggerId))
            {
                // Register runtime-trigger so it remains hidden across scene loads even before explicit Save()
                try
                {
                    SaveLoadSystem.RegisterRuntimeTriggered(triggerId);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[MainStoryTrigger] Could not persist trigger '{triggerId}': {ex.Message}", this);
                }
            }
        }
    }

    private System.Collections.IEnumerator TryTriggerWhenLoadFinishes()
    {
        while (SaveLoadSystem.IsLoadInProgress)
            yield return null;

        TryTrigger();
    }

    public static void TryTriggerAnyOverlappingPlayerTriggers()
    {
        var player = PlayerController.Instance != null
            ? PlayerController.Instance.gameObject
            : GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        var playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;

        var triggers = FindObjectsOfType<MainStoryTrigger>(true);
        for (int i = 0; i < triggers.Length; i++)
        {
            var trigger = triggers[i];
            if (trigger == null || !trigger.triggerOnPlayerEnter)
                continue;

            var triggerCollider = trigger.GetComponent<Collider2D>();
            if (triggerCollider == null)
                continue;

            if (!triggerCollider.bounds.Intersects(playerCollider.bounds))
                continue;

            if (!trigger.CanTriggerNow())
                continue;

            trigger.TryTrigger();
            if (MainStoryDirector.Instance != null && MainStoryDirector.Instance.IsPlayingStep)
                return;
        }
    }

    private System.Collections.IEnumerator TryTriggerIfPlayerAlreadyInside()
    {
        yield return null;

        if (!triggerOnPlayerEnter || (oneShot && hasTriggered))
            yield break;

        var ownCollider = GetComponent<Collider2D>();
        var player = PlayerController.Instance != null
            ? PlayerController.Instance.gameObject
            : GameObject.FindGameObjectWithTag("Player");
        if (ownCollider == null || player == null)
            yield break;

        var playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            yield break;

        if (ownCollider.bounds.Intersects(playerCollider.bounds) && CanTriggerNow())
            TryTrigger();
    }

    private void TryTriggerIfPlayerOverlappingNow()
    {
        var ownCollider = GetComponent<Collider2D>();
        var player = PlayerController.Instance != null
            ? PlayerController.Instance.gameObject
            : GameObject.FindGameObjectWithTag("Player");
        if (ownCollider == null || player == null)
            return;

        var playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;

        if (ownCollider.bounds.Intersects(playerCollider.bounds))
            TryTrigger();
    }

    public bool CanTriggerNow()
    {
        if (oneShot && hasTriggered)
            return false;

        if (restrictToScene && !string.IsNullOrWhiteSpace(requiredSceneName))
        {
            var activeSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.Equals(activeSceneName, requiredSceneName, System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var director = MainStoryDirector.Instance;
        if (director == null)
            return false;

        return director.CanTrigger(triggerId);
    }

    // Public accessors for Save/Load
    public string TriggerId => triggerId;
    public bool IsOneShot => oneShot;
    public bool HasTriggered => hasTriggered;

    // Apply triggered state from save data or external system. Hides visual instantly.
    public void ApplyTriggeredState(bool triggered)
    {
        if (!oneShot) return;
        if (string.IsNullOrWhiteSpace(triggerId)) return;

        if (triggered && ShouldIgnoreSavedTriggeredState())
            return;

        hasTriggered = triggered;
        var triggerVisual = GetComponentInChildren<StoryTriggerVisual>();
        if (triggerVisual != null)
        {
            if (triggered)
                triggerVisual.ForceHideInstant();
            else
            {
                // If un-setting, ensure visual active and reset state
                triggerVisual.EnsureVisible();
            }
        }
    }
}
