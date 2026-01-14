using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static string NextSpawnPointId; // được set trước khi LoadScene

    private void Awake()
    {
        // Tìm spawn point theo ID
        if (!string.IsNullOrEmpty(NextSpawnPointId))
        {
            var spawnObj = GameObject.Find("Spawn_" + NextSpawnPointId);
            var player = GameObject.FindGameObjectWithTag("Player");
            if (spawnObj != null && player != null)
            {
                player.transform.position = spawnObj.transform.position;

                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;

                var controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.StopAllCoroutines();
                    controller.isMoving = false;
                    controller.animator.SetFloat("Speed", 0f);
                } 

                var animator = player.GetComponent<Animator>();
                if (animator != null) animator.SetFloat("Speed", 0f);
            }

            // Dùng xong thì xóa để tránh dùng lại nhầm
            NextSpawnPointId = null;
        }
    }
}