using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class StoryFlagBlocker : MonoBehaviour
{
    [Header("Block Condition")]
    [SerializeField] private StoryFlagKey requiredStoryFlag = StoryFlagKey.StarterChosen;
    [SerializeField] private bool requiredValue = true;

    [Header("Block Behavior")]
    [SerializeField] private bool disableVisualWhenOpened = false;
    [SerializeField] private bool includeChildColliders = true;

    [Header("Blocked Reminder")]
    [SerializeField] private string blockedReminder = "Ban chua the di qua day.";
    [SerializeField] private float blockedReminderCooldown = 1f;

    private readonly List<Collider2D> trackedColliders = new List<Collider2D>();
    private readonly List<Renderer> trackedRenderers = new List<Renderer>();
    private bool isOpen;
    private float nextReminderTime;
    private Coroutine refreshRoutine;

    private void Awake()
    {
        CollectTargets();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshState();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isActiveAndEnabled)
            return;

        if (refreshRoutine != null)
            StopCoroutine(refreshRoutine);

        refreshRoutine = StartCoroutine(RefreshAfterSceneLoad());
    }

    private IEnumerator RefreshAfterSceneLoad()
    {
        yield return null;
        CollectTargets();
        RefreshState();
        refreshRoutine = null;
    }

    private void Update()
    {
        RefreshState();
    }

    private void CollectTargets()
    {
        trackedColliders.Clear();
        trackedRenderers.Clear();

        if (includeChildColliders)
        {
            trackedColliders.AddRange(GetComponentsInChildren<Collider2D>(true));
            trackedRenderers.AddRange(GetComponentsInChildren<Renderer>(true));
        }
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                trackedColliders.Add(col);

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                trackedRenderers.Add(renderer);
        }
    }

    private void RefreshState()
    {
        if (trackedColliders.Count == 0)
            CollectTargets();

        bool shouldOpen = CheckConditionMet();
        if (isOpen == shouldOpen)
            return;

        isOpen = shouldOpen;

        for (int i = 0; i < trackedColliders.Count; i++)
        {
            var col = trackedColliders[i];
            if (col != null)
                col.enabled = !isOpen;
        }

        if (disableVisualWhenOpened)
        {
            for (int i = 0; i < trackedRenderers.Count; i++)
            {
                var r = trackedRenderers[i];
                if (r != null)
                    r.enabled = !isOpen;
            }
        }
    }

    private bool CheckConditionMet()
    {
        var flags = StoryFlags.Instance;
        if (flags == null)
            return false;

        return flags.GetFlag(requiredStoryFlag) == requiredValue;
    }

    private bool IsPlayer(Collider2D other)
    {
        return other != null && other.CompareTag("Player");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isOpen)
            return;

        var other = collision != null ? collision.collider : null;
        if (!IsPlayer(other))
            return;

        TryShowBlockedReminder();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen)
            return;

        if (!IsPlayer(other))
            return;

        TryShowBlockedReminder();
    }

    private void TryShowBlockedReminder()
    {
        if (Time.unscaledTime < nextReminderTime)
            return;

        nextReminderTime = Time.unscaledTime + Mathf.Max(0.1f, blockedReminderCooldown);

        if (DialogManager.Instance != null)
            DialogManager.Instance.ShowDialog(blockedReminder);
        else
            Debug.Log(blockedReminder);
    }
}
