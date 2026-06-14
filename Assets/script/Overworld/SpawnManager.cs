using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static string NextSpawnPointId; // được set trước khi LoadScene
    public static Vector2? NextFacingDirection;
    public static string ReturnSceneName;
    public static string ReturnSpawnPointId;

    public static void SetNextSpawnPoint(string spawnPointId)
    {
        NextSpawnPointId = spawnPointId;
    }

    public static void SetNextSpawnFacingDirection(Vector2 facingDirection)
    {
        NextFacingDirection = facingDirection;
    }

    public static void SetReturnLocation(string sceneName, string spawnPointId)
    {
        ReturnSceneName = string.IsNullOrWhiteSpace(sceneName) ? null : sceneName;
        ReturnSpawnPointId = string.IsNullOrWhiteSpace(spawnPointId) ? null : spawnPointId;
    }

    public static bool HasReturnLocation()
    {
        return !string.IsNullOrWhiteSpace(ReturnSceneName) && !string.IsNullOrWhiteSpace(ReturnSpawnPointId);
    }

    public static bool TryConsumeReturnLocation(out string sceneName, out string spawnPointId)
    {
        sceneName = ReturnSceneName;
        spawnPointId = ReturnSpawnPointId;

        if (!HasReturnLocation())
            return false;

        ReturnSceneName = null;
        ReturnSpawnPointId = null;
        return true;
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(NextSpawnPointId))
            return;

        var player = ResolvePlayer();
        var spawnTransform = ResolveSpawnTransform(NextSpawnPointId);

        if (spawnTransform != null && player != null)
            ApplySpawn(player, spawnTransform.position);

        // Dùng xong thì xóa để tránh dùng lại nhầm
        NextSpawnPointId = null;
        NextFacingDirection = null;
    }

    private GameObject ResolvePlayer()
    {
        var player = PlayerController.Instance != null ? PlayerController.Instance.gameObject : null;
        if (player != null)
            return player;

        return GameObject.FindGameObjectWithTag("Player");
    }

    private Transform ResolveSpawnTransform(string spawnPointId)
    {
        // New path: component-based spawn point IDs.
        var points = FindObjectsOfType<SpawnPoint>(true);
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            if (point == null || string.IsNullOrWhiteSpace(point.Id))
                continue;

            if (string.Equals(point.Id, spawnPointId, System.StringComparison.OrdinalIgnoreCase))
                return point.transform;
        }

        // Backward compatibility: legacy object naming convention.
        var legacy = GameObject.Find("Spawn_" + spawnPointId);
        return legacy != null ? legacy.transform : null;
    }

    private void ApplySpawn(GameObject player, Vector3 position)
    {
        player.transform.position = position;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;

        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.StopAllCoroutines();
            controller.isMoving = false;
            controller.SetFacingDirection(NextFacingDirection ?? Vector2.up);
        }

        var animator = player.GetComponent<Animator>();
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }
}
