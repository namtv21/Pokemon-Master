using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGate : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetSceneName;     // VD: "Route01"
    [SerializeField] private string targetSpawnPointId;  // VD: "FromTown01"

    [Header("Transition FX")]
    [SerializeField] private float fadeToBlackDuration = 0.5f;
    [SerializeField] private float fadeFromBlackDuration = 0.25f;

    [Header("Story Gate (optional)")]
    [SerializeField] private bool requireStarterChosen;
    [SerializeField] private bool requireStoryFlag;
    [SerializeField] private StoryFlagKey requiredStoryFlag = StoryFlagKey.StarterChosen;
    [SerializeField] private bool requiredStoryFlagValue = true;
    [SerializeField] private string blockedReminder = "Hay gap tien si Oak truoc da!";
    [SerializeField] private float blockedReminderCooldown = 1f;

    private bool isLoading = false;
    private float nextBlockedReminderTime;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (requireStarterChosen && !CanPassStoryGate())
        {
            TryShowBlockedReminder();
            return;
        }

        isLoading = true;

        if (GameController.Instance != null)
            GameController.Instance.LoadSceneWithFade(targetSceneName, targetSpawnPointId, fadeToBlackDuration, fadeFromBlackDuration);
        else
        {
            SpawnManager.SetNextSpawnPoint(targetSpawnPointId);
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private bool CanPassStoryGate()
    {
        var flags = StoryFlags.Instance;
        if (flags == null)
            return false;

        if (requireStarterChosen && !flags.StarterChosen)
            return false;

        if (requireStoryFlag)
        {
            var current = flags.GetFlag(requiredStoryFlag);
            if (current != requiredStoryFlagValue)
                return false;
        }

        return true;
    }

    private void TryShowBlockedReminder()
    {
        if (Time.unscaledTime < nextBlockedReminderTime)
            return;

        nextBlockedReminderTime = Time.unscaledTime + Mathf.Max(0.1f, blockedReminderCooldown);

        if (DialogManager.Instance != null)
            DialogManager.Instance.ShowDialog(blockedReminder);
        else
            Debug.Log(blockedReminder);
    }
}
