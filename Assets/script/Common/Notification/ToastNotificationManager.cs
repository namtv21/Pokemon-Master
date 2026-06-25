using System.Collections.Generic;
using UnityEngine;

public class ToastNotificationManager : MonoBehaviour
{
    public static ToastNotificationManager Instance { get; private set; }

    [SerializeField] private ToastNotificationItem itemPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private int maxQueue = 20;

    private readonly Queue<(string message, Color color, float? hold)> queue = new();
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveReferences();
        HideTemplateItems();

        if (container != null)
            container.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Show(string message)
    {
        Show(message, Color.white, null);
    }

    public void Show(string message, Color color, float? holdTime = null)
    {
        if (string.IsNullOrWhiteSpace(message)) return;
        ResolveReferences();
        if (itemPrefab == null || container == null)
        {
            Debug.LogWarning("ToastNotificationManager missing prefab/container.");
            return;
        }

        if (queue.Count >= maxQueue) return;
        queue.Enqueue((message, color, holdTime));

        if (!isShowing)
            PlayNext();
    }

    private void PlayNext()
    {
        if (queue.Count == 0)
        {
            isShowing = false;
            if (container != null)
                container.gameObject.SetActive(false);
            return;
        }

        isShowing = true;
        var data = queue.Dequeue();

        if (container != null && !container.gameObject.activeInHierarchy)
            container.gameObject.SetActive(true);

        var item = Instantiate(itemPrefab, container);
        item.transform.SetAsLastSibling();
        Canvas.ForceUpdateCanvases();
        item.gameObject.SetActive(true);
        item.OnFinished += PlayNext;
        item.Play(data.message, data.color, data.hold);
    }

    private void ResolveReferences()
    {
        if (itemPrefab == null)
            itemPrefab = GetComponentInChildren<ToastNotificationItem>(true);

        if (container == null)
        {
            // Prefer a regular child container, not a template item.
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child == null) continue;
                if (child.GetComponent<ToastNotificationItem>() != null) continue;

                container = child;
                break;
            }
        }

        if (container == null && itemPrefab != null)
            container = itemPrefab.transform.parent;

        if (container == null)
        {
            var go = new GameObject("ToastContainer", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            container = go.transform;
        }
    }

    private void HideTemplateItems()
    {
        if (container == null) return;

        var templates = container.GetComponentsInChildren<ToastNotificationItem>(true);
        foreach (var template in templates)
        {
            if (template == null) continue;

            // Nếu item này chính là template trong scene, tắt nó đi để chỉ còn clone runtime.
            if (itemPrefab != null && template == itemPrefab)
                template.gameObject.SetActive(false);
        }
    }
}