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
    private Vector2 walkingDirection = Vector2.zero;
    private bool isWalking;

    public Vector2 FacingDirection => facing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        host = CoroutineHost.GetOrCreate();
        facing = ResolveInitialFacingFromCurrentSprite();
        ShowIdle(facing);
    }

    private void OnDisable()
    {
        // Coroutine walk chạy trên CoroutineHost bền vững (DontDestroyOnLoad) nên khi NPC
        // bị huỷ/tắt lúc đổi scene, nó không tự dừng → phải chủ động dừng để tránh truy cập
        // SpriteRenderer đã huỷ (MissingReferenceException).
        isWalking = false;
        StopWalkRoutine();
    }

    public void SetFacing(Vector2 worldDirection, bool idle)
    {
        facing = NormalizeDirection(worldDirection, facing);

        if (idle)
        {
            isWalking = false;
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

        isWalking = false;
        StopWalkRoutine();
        ShowIdle(facing);
    }

    private void StartWalkRoutine(Vector2 direction)
    {
        if (spriteRenderer == null)
            return;

        if (host == null)
        {
            ShowIdle(direction);
            return;
        }

        if (isWalking && walkingDirection == direction && walkRoutine != null)
            return;

        isWalking = true;
        walkingDirection = direction;

        if (walkRoutine != null)
            host.StopCoroutine(walkRoutine);

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
            isWalking = false;
            yield break;
        }

        float delay = 1f / Mathf.Max(1f, framesPerSecond);
        int index = 0;

        while (isWalking && walkingDirection == direction)
        {
            if (spriteRenderer == null)   // NPC đã bị huỷ (vd đổi scene) → dừng an toàn
                yield break;
            spriteRenderer.sprite = frames[index % frames.Length];
            index++;
            yield return new WaitForSeconds(delay);
        }

        walkRoutine = null;
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

    private Vector2 ResolveInitialFacingFromCurrentSprite()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return Vector2.down;

        var current = spriteRenderer.sprite;
        if (current == idleUp)
            return Vector2.up;
        if (current == idleLeft)
            return Vector2.left;
        if (current == idleRight)
            return Vector2.right;
        if (current == idleDown)
            return Vector2.down;

        if (ContainsSprite(walkUpFrames, current))
            return Vector2.up;
        if (ContainsSprite(walkLeftFrames, current))
            return Vector2.left;
        if (ContainsSprite(walkRightFrames, current))
            return Vector2.right;
        if (ContainsSprite(walkDownFrames, current))
            return Vector2.down;

        return Vector2.down;
    }

    private static bool ContainsSprite(Sprite[] sprites, Sprite target)
    {
        if (sprites == null || target == null)
            return false;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] == target)
                return true;
        }

        return false;
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
