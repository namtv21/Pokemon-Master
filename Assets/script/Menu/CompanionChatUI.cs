using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompanionChatUI : MonoBehaviour
{
    [Header("Panel gốc")]
    [SerializeField] private GameObject panel;

    [Header("Thông tin companion")]
    [SerializeField] private Image companionSprite;
    [SerializeField] private TMP_Text intimacyText;
    [SerializeField] private TMP_Text statusText;

    [Header("Mood Sprites (0=mệt, 1=bình thường, 2=vui)")]
    [SerializeField] private Sprite[] moodSprites;

    [Header("Reaction Sprites theo topic (index khớp Topics[])")]
    [SerializeField] private Sprite[] reactionSprites; // null = dùng mood sprite mặc định

    [Header("Chọn chủ đề")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private TMP_Text[] optionTexts;

    [Header("Hiện phản hồi")]
    [SerializeField] private GameObject responsePanel;
    [SerializeField] private TMP_Text responseText;

    [Header("Nhập liệu Online")]
    [SerializeField] private GameObject inputPanel;
    [SerializeField] private TMP_InputField chatInputField;

    private static readonly string[] Topics =
    {
        "Tâm trạng",
        "Tiếp theo tôi nên làm gì?",
        "Suy nghĩ về Team Rocket",
        "Suy nghĩ về Green",
        "Suy nghĩ về Blue",
        "Trò chuyện (Online)"
    };

    private enum ChatState { ChoosingTopic, ShowingResponse, TypingMessage, WaitingForResponse }
    private ChatState state;
    private int selectedIndex;
    private Pokemon companion;
    private Coroutine animCoroutine;

    private void Awake() => panel?.SetActive(false);

    // --- Mở / Đóng ---

    public void OpenOfflineMode(Pokemon companion)
    {
        this.companion = companion;
        panel.SetActive(true);

        UpdateMoodSprite(CompanionChatSystem.Instance.GetMoodIndex(companion));
        RefreshIntimacy();

        if (statusText != null)
        {
            statusText.text = "Offline";
            statusText.color = Color.yellow;
        }

        ShowTopicSelection();
    }

    public void OpenOnlineMode(Pokemon companion)
    {
        OpenOfflineMode(companion);
        if (statusText != null)
        {
            statusText.text = "Online";
            statusText.color = Color.green;
        }
    }

    public void Close()
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        if (companionSprite != null) companionSprite.transform.localScale = Vector3.one;
        panel?.SetActive(false);
        companion = null;
    }

    // --- Update ---

    public void HandleUpdate()
    {
        if (panel == null || !panel.activeSelf) return;

        switch (state)
        {
            case ChatState.ChoosingTopic:      HandleTopicInput();    break;
            case ChatState.ShowingResponse:    HandleResponseInput(); break;
            case ChatState.TypingMessage:      HandleTypingInput();   break;
            case ChatState.WaitingForResponse: break; // chờ API — không nhận input
        }
    }

    // --- Logic nội bộ ---

    private void ShowTopicSelection()
    {
        state = ChatState.ChoosingTopic;
        selectedIndex = 0;

        optionsPanel?.SetActive(true);
        responsePanel?.SetActive(false);
        inputPanel?.SetActive(false);
        ApplyReactionSprite(-1);

        for (int i = 0; i < optionTexts.Length; i++)
        {
            bool active = i < Topics.Length;
            optionTexts[i].gameObject.SetActive(active);
            if (active) optionTexts[i].text = Topics[i];
        }

        HighlightOption();
    }

    private void HandleTopicInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex = (selectedIndex - 1 + Topics.Length) % Topics.Length;
            HighlightOption();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex = (selectedIndex + 1) % Topics.Length;
            HighlightOption();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            if (selectedIndex == Topics.Length - 1) // "Trò chuyện (Online)"
            {
                if (CompanionChatSystem.Instance.IsOnline)
                    OpenOnlineChatInput();
                else
                    ToastNotificationManager.Instance?.Show("Không thể dùng khi offline!", Color.yellow);
                return;
            }

            string response = CompanionChatSystem.Instance.GetOfflineResponse(companion, selectedIndex);
            RefreshIntimacy();

            ApplyReactionSprite(selectedIndex);

            if (selectedIndex == 0) // Tâm trạng — thêm animation
            {
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(PlayMoodAnimation(CompanionChatSystem.Instance.GetMoodIndex(companion)));
            }

            ShowResponse(response);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Close();
            MenuController.Instance?.OpenMainMenu();
        }
    }

    private void ShowResponse(string response)
    {
        state = ChatState.ShowingResponse;
        optionsPanel?.SetActive(false);
        responsePanel?.SetActive(true);

        if (responseText != null)
            responseText.text = response + "\n\n<size=70%><color=#aaa>[Z] Tiếp  [X] Thoát</color></size>";
    }

    private void HandleResponseInput()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            ShowTopicSelection();
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Close();
            MenuController.Instance?.OpenMainMenu();
        }
    }

    private void OpenOnlineChatInput()
    {
        state = ChatState.TypingMessage;
        optionsPanel?.SetActive(false);
        responsePanel?.SetActive(false);
        inputPanel?.SetActive(true);

        if (chatInputField != null)
        {
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    private void HandleTypingInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            string msg = chatInputField != null ? chatInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(msg)) return;
            chatInputField.text = "";
            inputPanel?.SetActive(false);
            StartCoroutine(SendOnlineMessage(msg));
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            inputPanel?.SetActive(false);
            ShowTopicSelection();
        }
    }

    private IEnumerator SendOnlineMessage(string userMessage)
    {
        state = ChatState.WaitingForResponse;
        if (responsePanel != null) responsePanel.SetActive(true);
        if (responseText != null)
            responseText.text = "Đang nhập...\n\n<size=70%><color=#aaa>Vui lòng chờ</color></size>";

        yield return CompanionChatSystem.Instance.SendMessageToCompanion(userMessage, response =>
        {
            ShowResponse(response);
        });
    }

    // --- Sprite & Animation ---

    private void UpdateMoodSprite(int moodIndex)
    {
        if (companionSprite == null) return;
        Sprite s = null;
        if (moodSprites != null && moodIndex < moodSprites.Length)
            s = moodSprites[moodIndex];
        companionSprite.sprite = s != null ? s : companion?.Base?.FrontSprite;
    }

    private IEnumerator PlayMoodAnimation(int moodIndex)
    {
        if (companionSprite == null) yield break;
        var t = companionSprite.transform;
        var original = t.localScale;

        if (moodIndex == 2) // vui — nhảy 2 lần
        {
            for (int i = 0; i < 2; i++)
            {
                yield return ScaleTo(t, original * 1.35f, 0.1f);
                yield return ScaleTo(t, original, 0.1f);
            }
        }
        else if (moodIndex == 0) // mệt — thu nhỏ rồi phục hồi
        {
            yield return ScaleTo(t, original * 0.8f, 0.25f);
            yield return ScaleTo(t, original, 0.2f);
        }
        else // bình thường — nhảy nhẹ 1 lần
        {
            yield return ScaleTo(t, original * 1.15f, 0.1f);
            yield return ScaleTo(t, original, 0.1f);
        }

        t.localScale = original;
        animCoroutine = null;
    }

    private IEnumerator ScaleTo(Transform t, Vector3 target, float duration)
    {
        var start = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        t.localScale = target;
    }

    // --- Helpers ---

    // Đổi sprite companion theo topic; index -1 hoặc không có sprite → dùng mood sprite
    private void ApplyReactionSprite(int topicIndex)
    {
        if (companionSprite == null) return;
        Sprite reaction = (topicIndex >= 0 && reactionSprites != null && topicIndex < reactionSprites.Length)
            ? reactionSprites[topicIndex]
            : null;
        if (reaction != null)
            companionSprite.sprite = reaction;
        else
            UpdateMoodSprite(CompanionChatSystem.Instance.GetMoodIndex(companion));
    }

    private void RefreshIntimacy()
    {
        if (intimacyText != null)
            intimacyText.text = $"Lv thân mật: {CompanionChatSystem.Instance.IntimacyLevel}";
    }

    private void HighlightOption()
    {
        for (int i = 0; i < optionTexts.Length; i++)
        {
            if (optionTexts[i] != null && optionTexts[i].gameObject.activeSelf)
                optionTexts[i].color = i == selectedIndex ? Color.yellow : Color.white;
        }
    }
}
