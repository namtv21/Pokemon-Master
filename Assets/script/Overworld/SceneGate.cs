using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneGate : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetSceneName;     // VD: "Route01"
    [SerializeField] private string targetSpawnPointId;  // VD: "FromTown01"

    private bool isLoading = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading) return;
        if (other.CompareTag("Player"))
        {
            isLoading = true;
            SpawnManager.NextSpawnPointId = targetSpawnPointId;
            SceneManager.LoadScene(targetSceneName);
        }
    }
}