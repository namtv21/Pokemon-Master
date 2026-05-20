using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class NPCDirectionalAnimator : MonoBehaviour
{
    [Header("Idle Sprites")]
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;
    [SerializeField] private Sprite idleUp;

    [Header("Walk Frames")]
    [SerializeField] private Sprite[] walkDownFrames;
    [SerializeField] private Sprite[] walkLeftFrames;
    [SerializeField] private Sprite[] walkRightFrames;
    [SerializeField] private Sprite[] walkUpFrames;

    [Header("Timing")]
    [SerializeField] private float framesPerSecond = 8f;

    private SpriteRenderer spriteRenderer;
    private Coroutine walkRoutine;
    private CoroutineHost host;
    private Vector2 facing = Vector2.down;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        host = CoroutineHost.GetOrCreate();
        ShowIdle(facing);
    }

    public void SetFacing(Vector2 worldDirection, bool idle)
    {
        facing = NormalizeDirection(worldDirection, facing);

        if (idle)
        {
            StopWalkRoutine();
            ShowIdle(facing);
            return;
        }

        StartWalkRoutine(facing);
    }

    public void SetMoving(bool moving, Vector2 worldDirection)
    {
        facing = NormalizeDirection(worldDirection, facing);

        if (moving)
        {
            StartWalkRoutine(facing);
            return;
        }

        StopWalkRoutine();
        ShowIdle(facing);
    }

    private void StartWalkRoutine(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        if (walkRoutine != null && host != null)
            host.StopCoroutine(walkRoutine);

        if (host == null)
        {
            ShowIdle(direction);
            return;
        }

        walkRoutine = host.StartCoroutine(AnimateWalk(direction));
    }

    private void StopWalkRoutine()
    {
        if (walkRoutine != null && host != null)
        {
            host.StopCoroutine(walkRoutine);
            walkRoutine = null;
        }
    }

    private IEnumerator AnimateWalk(Vector2 direction)
    {
        var frames = ResolveWalkFrames(direction);
        if (frames == null || frames.Length == 0)
        {
            ShowIdle(direction);
            walkRoutine = null;
            yield break;
        }

        float delay = 1f / Mathf.Max(1f, framesPerSecond);
        int index = 0;

        while (true)
        {
            spriteRenderer.sprite = frames[index % frames.Length];
            index++;
            yield return new WaitForSeconds(delay);
        }
    }

    private void ShowIdle(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite = ResolveIdleSprite(direction);
    }

    private Sprite ResolveIdleSprite(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? idleRight : idleLeft;

        return direction.y >= 0f ? idleUp : idleDown;
    }

    private Sprite[] ResolveWalkFrames(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x >= 0f ? walkRightFrames : walkLeftFrames;

        return direction.y >= 0f ? walkUpFrames : walkDownFrames;
    }

    private Vector2 NormalizeDirection(Vector2 worldDirection, Vector2 fallback)
    {
        if (worldDirection.sqrMagnitude < 0.0001f)
            return fallback.sqrMagnitude < 0.0001f ? Vector2.down : fallback;

        if (Mathf.Abs(worldDirection.x) > Mathf.Abs(worldDirection.y))
            return worldDirection.x >= 0f ? Vector2.right : Vector2.left;

        return worldDirection.y >= 0f ? Vector2.up : Vector2.down;
    }

    private sealed class CoroutineHost : MonoBehaviour
    {
        private static CoroutineHost instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetInstance()
        {
            instance = null;
        }

        public static CoroutineHost GetOrCreate()
        {
            if (instance != null)
                return instance;

            var existing = FindObjectOfType<CoroutineHost>();
            if (existing != null)
            {
                instance = existing;
                return instance;
            }

            var go = new GameObject("NPCDirectionalAnimatorRunner");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<CoroutineHost>();
            return instance;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}