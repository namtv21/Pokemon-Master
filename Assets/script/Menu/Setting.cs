using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider volumeSlider;

    [Header("Gán chữ 'Audio' trong scene vào đây để hiện dấu mũi tên")]
    [SerializeField] private TMP_Text volumeLabel;

    [Header("Display (optional — gán label từ scene hoặc để trống để tự tạo)")]
    [SerializeField] private TMP_Text displayModeLabel;

    private int volumeLevel = 1;
    private const int maxLevel = 9;

    // 0 = dòng âm lượng, 1 = dòng màn hình
    private int selectedRow = 0;

    // Các chế độ màn hình: (tên hiển thị, width, height, fullscreen)
    private static readonly (string label, int w, int h, bool fs)[] DisplayModes =
    {
        ("Toàn màn hình",   1920, 1080, true),
        ("Cửa sổ 1920×1080", 1920, 1080, false),
        ("Cửa sổ 1280×720",  1280,  720, false),
        ("Cửa sổ 960×540",    960,  540, false),
    };

    private int displayModeIndex = 2;

    public void Open() => gameObject.SetActive(true);
    public void Close()
    {
        selectedRow = 0;
        gameObject.SetActive(false);
    }

    void Start()
    {
        // Âm lượng
        volumeLevel = PlayerPrefs.GetInt("MusicVolumeLevel", 1);
        float normalizedVolume = volumeLevel / (float)maxLevel;
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(normalizedVolume);
        if (volumeSlider != null)
            volumeSlider.value = normalizedVolume;
        volumeSlider?.onValueChanged.AddListener(SetVolume);

        // Màn hình
        displayModeIndex = PlayerPrefs.GetInt("DisplayModeIndex", 2);
        displayModeIndex = Mathf.Clamp(displayModeIndex, 0, DisplayModes.Length - 1);
        ApplyDisplayMode(displayModeIndex);
        UpdateDisplayLabel();
    }

    void OnEnable()
    {
        selectedRow = 0;
        UpdateDisplayLabel();
    }

    public void HandleUpdate(System.Action onClose)
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            onClose?.Invoke();
            return;
        }

        // Up/Down chuyển giữa dòng âm lượng và dòng màn hình
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedRow = selectedRow == 0 ? 1 : 0;
            UpdateDisplayLabel();
            return;
        }

        if (selectedRow == 0)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                volumeLevel = Mathf.Min(volumeLevel + 1, maxLevel);
                ApplyVolume();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                volumeLevel = Mathf.Max(volumeLevel - 1, 0);
                ApplyVolume();
            }
        }
        else
        {
            // Left/Right cycle qua các chế độ màn hình
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            {
                displayModeIndex = (displayModeIndex + 1) % DisplayModes.Length;
                ApplyDisplayMode(displayModeIndex);
                UpdateDisplayLabel();
            }
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                displayModeIndex = (displayModeIndex - 1 + DisplayModes.Length) % DisplayModes.Length;
                ApplyDisplayMode(displayModeIndex);
                UpdateDisplayLabel();
            }
        }
    }

    private void ApplyVolume()
    {
        float normalizedVolume = volumeLevel / (float)maxLevel;
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(normalizedVolume);
        if (volumeSlider != null)
            volumeSlider.value = normalizedVolume;
        PlayerPrefs.SetInt("MusicVolumeLevel", volumeLevel);
        PlayerPrefs.Save();
        UpdateDisplayLabel();
    }

    public void SetVolume(float value)
    {
        volumeLevel = Mathf.RoundToInt(value * maxLevel);
        ApplyVolume();
    }

    private void ApplyDisplayMode(int index)
    {
        var mode = DisplayModes[index];
        Screen.SetResolution(mode.w, mode.h, mode.fs);
        PlayerPrefs.SetInt("DisplayModeIndex", index);
        PlayerPrefs.Save();
    }

    private void UpdateDisplayLabel()
    {
        if (volumeLabel != null)
        {
            string vc = selectedRow == 0 ? "► " : "  ";
            volumeLabel.text = $"{vc}Audio";
        }

        EnsureDisplayLabel();
        if (displayModeLabel == null) return;

        string cursor = selectedRow == 1 ? "► " : "  ";
        string modeName = DisplayModes[displayModeIndex].label;
        displayModeLabel.text = $"{cursor}Màn hình: ◄ {modeName} ► ";
    }

    private void EnsureDisplayLabel()
    {
        if (displayModeLabel != null) return;

        Transform parent = volumeSlider != null ? volumeSlider.transform.parent : transform;
        if (parent == null) parent = transform;

        var go = new GameObject("DisplayModeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(0f, 40f);

        displayModeLabel = go.GetComponent<TextMeshProUGUI>();
        displayModeLabel.fontSize = 22f;
        displayModeLabel.color = Color.white;
        displayModeLabel.alignment = TextAlignmentOptions.Left;
        if (TMP_Settings.defaultFontAsset != null)
            displayModeLabel.font = TMP_Settings.defaultFontAsset;
    }
}
