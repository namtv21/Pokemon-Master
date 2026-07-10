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
        "Vuốt ve",
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

        UpdateCompanionSprite();
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
        UpdateCompanionSprite();

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

            if (selectedIndex == Topics.Length - 2) // "Vuốt ve" — tăng bond, offline
            {
                string petResp = CompanionChatSystem.Instance.PetCompanion(companion);
                RefreshIntimacy();
                UpdateCompanionSprite();
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(PlayMoodAnimation(2)); // vui
                ShowResponse(petResp);
                return;
            }

            string response = CompanionChatSystem.Instance.GetOfflineResponse(companion, selectedIndex);
            RefreshIntimacy();

            UpdateCompanionSprite();

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
            if (msg.Length > 200) msg = msg.Substring(0, 200);   // cap độ dài input
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

    private void UpdateCompanionSprite()
    {
        if (companionSprite == null || companion == null) return;
        companionSprite.sprite = companion.Base.FrontSprite;
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

    private void RefreshIntimacy()
    {
        if (intimacyText == null) return;

        intimacyText.text = companion != null
            ? CompanionChatSystem.Instance.GetStatusSummary(companion)      // "Tinh nghịch · Bạn (55) · vui vẻ"
            : $"Lv thân mật: {CompanionChatSystem.Instance.IntimacyLevel}";
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
