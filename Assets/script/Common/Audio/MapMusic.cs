using UnityEngine;

public class MapMusic : MonoBehaviour
{
    [SerializeField] private AudioClip mapClip;

    void Start()
    {
        MusicManager.Instance.PlayMusic(mapClip, isMapMusic: true);
    }
}