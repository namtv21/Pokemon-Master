using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    private int volumeLevel = 2;
    private const int maxLevel = 9;

    public void Open() => gameObject.SetActive(true);  
    public void Close() => gameObject.SetActive(false);

    void Start()
    {
        volumeLevel = PlayerPrefs.GetInt("MusicVolumeLevel", 2);
        float normalizedVolume = volumeLevel / (float)maxLevel;

        MusicManager.Instance.SetVolume(normalizedVolume);
        volumeSlider.value = normalizedVolume;

        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void HandleUpdate(System.Action onClose) 
    { 
        if (Input.GetKeyDown(KeyCode.X)) 
        { 
            onClose?.Invoke(); 
        }

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

    private void ApplyVolume()
    {
        float normalizedVolume = volumeLevel / (float)maxLevel;
        MusicManager.Instance.SetVolume(normalizedVolume);
        volumeSlider.value = normalizedVolume;

        PlayerPrefs.SetInt("MusicVolumeLevel", volumeLevel);
        PlayerPrefs.Save();
    }

    public void SetVolume(float value)
    {
        volumeLevel = Mathf.RoundToInt(value * maxLevel);
        ApplyVolume();
    }
}