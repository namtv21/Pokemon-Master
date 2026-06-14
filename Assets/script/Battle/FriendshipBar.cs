using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendshipBar : MonoBehaviour
{
    [Header("Legacy fill (optional)")]
    [SerializeField] private RectTransform friendshipFill;

    [SerializeField] private TextMeshProUGUI friendshipText;

    // Sets a continuous fraction (0..1) - kept for backward compatibility
    public void SetFriendshipFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        if (friendshipFill != null)
            friendshipFill.localScale = new Vector3(fraction, 1f, 1f);
    }

    // Sets discrete tick visuals: friendshipLevel and progress (0..maxProgress)
    public void SetFriendshipNumbers(int friendshipLevel, int currentProgress, int maxProgress)
    {
        // Use the old continuous fill bar only.
        if (friendshipFill != null && maxProgress > 0)
            friendshipFill.localScale = new Vector3(Mathf.Clamp01((float)currentProgress / maxProgress), 1f, 1f);

        if (friendshipText != null)
            friendshipText.text = $"Lv {Mathf.Max(0, friendshipLevel)}";
    }
}
