using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, Interactable
{
    [Header("Reward")]
    [SerializeField] private ItemBase itemReward;
    [SerializeField] private int moneyReward = 0;
    [SerializeField] private bool givesBadge = false;
    [SerializeField] private string badgeId;

    [Header("Visual")]
    [SerializeField] private Sprite Sprite;

    private bool opened = false;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();

        // Auto-add collider to block movement
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
        }
    }

    public void Interact()
    {
        if (opened)
        {
            DialogManager.Instance?.ShowDialog("It's empty.");
            return;
        }

        opened = true;

        if (givesBadge && !string.IsNullOrWhiteSpace(badgeId))
        {
            // Persist badge id to PlayerPrefs under key "PlayerBadges"
            const string prefsKey = "PlayerBadges";
            var data = PlayerPrefs.GetString(prefsKey, string.Empty);
            var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(data))
            {
                var parts = data.Split(new[] {'|'}, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) set.Add(p);
            }
            if (!set.Contains(badgeId))
            {
                set.Add(badgeId);
                PlayerPrefs.SetString(prefsKey, string.Join("|", set));
                PlayerPrefs.Save();
            }

            DialogManager.Instance?.ShowDialog($"You found a badge: {badgeId}!");
        }
        else if (itemReward != null)
        {
            Inventory.Instance?.AddItem(itemReward, 1);
            DialogManager.Instance?.ShowDialog($"You found {itemReward.name}!");
        }
        else if (moneyReward > 0)
        {
            Inventory.Instance?.AddMoney(moneyReward);
            DialogManager.Instance?.ShowDialog($"You found {moneyReward} Yen!");
        }
        else
        {
            DialogManager.Instance?.ShowDialog("The chest is empty.");
        }
    }
}
