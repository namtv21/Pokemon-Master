using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;   // Slider trong Menu Setting

    public void Open() => gameObject.SetActive(true); 
    public void Close() => gameObject.SetActive(false);
    void Start()
    {
        // Lấy giá trị đã lưu hoặc mặc định 0.5
        float savedVolume = PlayerPrefs.GetFloat("MusicVolume", 0.5f);
        MusicManager.Instance.SetVolume(savedVolume);
        volumeSlider.value = savedVolume;

        // Lắng nghe sự kiện thay đổi slider
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }
    public void HandleUpdate(System.Action onClose) 
    { 
        if (Input.GetKeyDown(KeyCode.X)) 
        { 
            onClose?.Invoke(); 
        }
    }
    public void SetVolume(float value)
    {
        MusicManager.Instance.SetVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save(); // đảm bảo lưu xuống ổ cứng
    }
}
