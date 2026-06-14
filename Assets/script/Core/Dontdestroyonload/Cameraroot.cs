using UnityEngine.SceneManagement;
using Cinemachine;
using UnityEngine;

public class CameraBootstrap : MonoBehaviour
{
    private void Start()
    {
        var vcam = GetComponent<CinemachineVirtualCamera>();
        var player = GameObject.FindGameObjectWithTag("Player");
        if (vcam != null && player != null)
            vcam.Follow = player.transform;
    }
}