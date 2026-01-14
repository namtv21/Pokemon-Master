using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;
    private AudioSource audioSource;
    private AudioClip lastMapClip;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            audioSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic(AudioClip clip, bool isMapMusic = false)
    {
        if (audioSource.clip == clip) return; // tránh restart nếu cùng clip
        if (clip == null)
        {
            // Nếu null → phát lại nhạc map trước đó
            if (lastMapClip != null)
            {
                audioSource.clip = lastMapClip;
                audioSource.loop = true;
                audioSource.Play();
            }
            return;
        }
        if (isMapMusic)
        {
            lastMapClip = clip; // lưu lại nhạc map cuối cùng
        }
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }
    public void SetVolume(float value) 
    { 
        audioSource.volume = value; 
    }

}