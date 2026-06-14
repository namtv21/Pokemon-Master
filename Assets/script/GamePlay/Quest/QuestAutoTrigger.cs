using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestAutoTrigger : MonoBehaviour
{
    [Header("When To Trigger")]
    [SerializeField] private bool triggerOnSceneStart = false;
    [SerializeField] private bool triggerOnPlayerEnter = true;
    [SerializeField] private bool oneShot = true;

    [Header("Conditions")]
    [SerializeField] private bool requirePrologueDone = false;
    [SerializeField] private bool restrictToScene = false;
    [SerializeField] private string requiredSceneName;
    [SerializeField] private bool onlyWhenCurrentMainStoryActive = false;

    [Header("Quest Accept")]
    [SerializeField] private bool autoAcceptQuest = false;
    [SerializeField] private bool useCurrentMainStoryQuest = true;
    [SerializeField] private Quest questToAccept;
    [SerializeField] private bool acceptOnceOnly = true;

    [Header("Quest Event")]
    [SerializeField] private bool submitQuestEvent = true;
    [SerializeField] private QuestEventType eventType = QuestEventType.LocationReached;
    [SerializeField] private string targetId;
    [SerializeField] private int amount = 1;

    private bool hasTriggered;

    private void Start()
    {
        if (triggerOnSceneStart)
            TryTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnPlayerEnter) return;
        if (!other.CompareTag("Player")) return;

        TryTrigger();
    }

    private void TryTrigger()
    {
        if (oneShot && hasTriggered)
            return;

        if (restrictToScene && !string.IsNullOrWhiteSpace(requiredSceneName))
        {
            var activeScene = SceneManager.GetActiveScene().name;
            if (!string.Equals(activeScene, requiredSceneName, System.StringComparison.OrdinalIgnoreCase))
                return;
        }

        var qm = QuestManager.Instance;
        if (qm == null)
            return;

        if (onlyWhenCurrentMainStoryActive)
        {
            var mainQuest = qm.GetCurrentMainStoryQuest();
            if (mainQuest == null || !qm.IsQuestActive(mainQuest))
                return;
        }

        if (autoAcceptQuest)
        {
            var quest = ResolveQuestToAccept(qm);
            if (quest != null)
                qm.AddQuest(quest, acceptOnceOnly);
        }

        if (submitQuestEvent)
        {
            var resolvedTarget = ResolveTargetId();
            qm.SubmitEvent(new QuestEvent(eventType, resolvedTarget, Mathf.Max(1, amount)));
        }

        hasTriggered = true;
    }

    private Quest ResolveQuestToAccept(QuestManager qm)
    {
        if (!useCurrentMainStoryQuest)
            return questToAccept;

        var currentMain = qm.GetCurrentMainStoryQuest();
        return currentMain != null ? currentMain : questToAccept;
    }

    private string ResolveTargetId()
    {
        if (!string.IsNullOrWhiteSpace(targetId))
            return targetId;

        if (triggerOnSceneStart)
            return SceneManager.GetActiveScene().name;

        return gameObject.name;
    }
}
