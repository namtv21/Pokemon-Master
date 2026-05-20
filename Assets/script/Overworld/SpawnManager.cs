using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static string NextSpawnPointId; // được set trước khi LoadScene

    public static void SetNextSpawnPoint(string spawnPointId)
    {
        NextSpawnPointId = spawnPointId;
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
            if (controller.animator != null)
                controller.animator.SetFloat("Speed", 0f);
        }

        var animator = player.GetComponent<Animator>();
        if (animator != null)
            animator.SetFloat("Speed", 0f);
    }
}