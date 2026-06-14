using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class DoorTransition : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnPointId;

    [Header("Shared Scene Return")]
    [SerializeField] private bool rememberReturnLocation;
    [SerializeField] private string returnSpawnPointId;
    [SerializeField] private bool useSavedReturnLocation;

    [Header("Transition FX")]
    [SerializeField] private float fadeToBlackDuration = 0.5f;
    [SerializeField] private float fadeFromBlackDuration = 0.25f;

    [Header("Interaction")]
    [SerializeField] private bool requireInteractKey = true;
    [SerializeField] private KeyCode interactKey = KeyCode.Z;

    private bool isPlayerInside;
    private bool isLoading;
    private Collider2D doorCollider;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        doorCollider = GetComponent<Collider2D>();
    }

    private void Update()
    {
        if (isLoading || !isPlayerInside)
            return;

        if (GameController.Instance != null && GameController.Instance.State != GameState.Overworld)
            return;

        var dialogManager = DialogManager.Instance;
        if (dialogManager != null && (dialogManager.IsShowing || dialogManager.IsDebouncingInput))
            return;

        if (!IsPlayerFacingDoor())
            return;

        if (!requireInteractKey || Input.GetKeyDown(interactKey))
            Transition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInside = true;

        if (!requireInteractKey)
            Transition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInside = false;
    }

    private void Transition()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            if (!useSavedReturnLocation)
            {
                Debug.LogWarning("DoorTransition missing targetSceneName.");
                return;
            }
        }

        isLoading = true;

        if (rememberReturnLocation)
        {
            var currentScene = SceneManager.GetActiveScene().name;
            SpawnManager.SetReturnLocation(currentScene, returnSpawnPointId);
        }

        if (useSavedReturnLocation && SpawnManager.TryConsumeReturnLocation(out var returnSceneName, out var returnSpawnPoint))
        {
            SpawnManager.SetNextSpawnFacingDirection(Vector2.up);
            if (GameController.Instance != null)
                GameController.Instance.LoadSceneWithFade(returnSceneName, returnSpawnPoint, fadeToBlackDuration, fadeFromBlackDuration);
            else
            {
                SpawnManager.SetNextSpawnPoint(returnSpawnPoint);
                SceneManager.LoadScene(returnSceneName);
            }

            return;
        }

        SpawnManager.SetNextSpawnFacingDirection(Vector2.up);
        if (GameController.Instance != null)
            GameController.Instance.LoadSceneWithFade(targetSceneName, targetSpawnPointId, fadeToBlackDuration, fadeFromBlackDuration);
        else
        {
            SpawnManager.SetNextSpawnPoint(targetSpawnPointId);
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private bool IsPlayerFacingDoor()
    {
        var player = PlayerController.Instance;
        if (player == null)
            return false;

        Vector2 playerPos = player.GetPosition();
        Vector2 doorPos = doorCollider != null ? doorCollider.bounds.center : (Vector2)transform.position;
        Vector2 toDoor = doorPos - playerPos;

        if (toDoor.sqrMagnitude < 0.0001f)
            return true;

        Vector2 facing = player.GetFacingDirection();
        return Vector2.Dot(facing.normalized, toDoor.normalized) > 0.5f;
    }
}
