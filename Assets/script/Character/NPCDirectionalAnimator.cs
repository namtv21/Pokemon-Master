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

    [Header("Idle Look (tuỳ chọn)")]
    [Tooltip("Bật để NPC thỉnh thoảng quay đầu nhìn quanh HOẶC giậm chân tại chỗ khi đứng yên (kiểu Pokemon gốc). Để TẮT với NPC cần cố định hướng (y tá, lính gác...).")]
    [SerializeField] private bool randomIdleLook = false;
    [SerializeField] private Vector2 idleLookInterval = new Vector2(3f, 7f);
    [Range(0f, 1f)]
    [Tooltip("Xác suất hành vi idle là GIẬM CHÂN tại chỗ (phần còn lại là quay đầu).")]
    [SerializeField] private float stepInPlaceChance = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Coroutine walkRoutine;
    private Vector2 facing = Vector2.down;
    private Vector2 walkingDirection = Vector2.zero;
    private bool isWalking;
    private float nextLookTime;

    public Vector2 FacingDirection => facing;

    private static readonly Vector2[] LookDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        facing = ResolveInitialFacingFromCurrentSprite();
        ShowIdle(facing);
        nextLookTime = Time.time + Random.Range(idleLookInterval.x, idleLookInterval.y);
    }

    private bool steppingInPlace;

    // NPC "sống": thỉnh thoảng đổi hướng nhìn hoặc giậm chân tại chỗ khi đứng yên ngoài overworld.
    // Không chạy khi đang đi, đang thoại/cutscene (state != Overworld) — tránh phá dàn cảnh.
    private void Update()
    {
        if (!randomIdleLook || isWalking || steppingInPlace)
            return;

        var gc = GameController.Instance;
        if (gc == null || gc.State != GameState.Overworld)
            return;

        if (Time.time < nextLookTime)
            return;

        nextLookTime = Time.time + Random.Range(idleLookInterval.x, idleLookInterval.y);

        if (Random.value < stepInPlaceChance)
            StartCoroutine(StepInPlaceRoutine());
        else
        {
            facing = LookDirections[Random.Range(0, LookDirections.Length)];
            ShowIdle(facing);
        }
    }

    // Giậm chân tại chỗ: chạy walk frames theo hướng hiện tại một lúc ngắn, KHÔNG di chuyển.
    // Nếu movement thật ghi đè (SetMoving/SetFacing xoá cờ) thì nhường quyền, không tự dừng.
    private IEnumerator StepInPlaceRoutine()
    {
        steppingInPlace = true;
        StartWalkRoutine(facing);
        yield return new WaitForSeconds(Random.Range(0.6f, 1.2f));

        if (steppingInPlace)
        {
            isWalking = false;
            StopWalkRoutine();
            ShowIdle(facing);
        }
        steppingInPlace = false;
    }

    // Coroutine chạy trên chính NPC → tắt/hủy object là Unity tự dừng, chỉ cần dọn cờ.
    private void OnDisable()
    {
        isWalking = false;
        walkRoutine = null;
        steppingInPlace = false;
    }

    public void SetFacing(Vector2 worldDirection, bool idle)
    {
        steppingInPlace = false;   // lệnh thật ghi đè hành vi idle
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
        steppingInPlace = false;   // lệnh thật ghi đè hành vi idle
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
        // Object đang tắt thì không start coroutine được — hiện idle là đủ.
        if (!isActiveAndEnabled)
        {
            ShowIdle(direction);
            return;
        }

        if (isWalking && walkingDirection == direction && walkRoutine != null)
            return;

        isWalking = true;
        walkingDirection = direction;

        StopWalkRoutine();
        walkRoutine = StartCoroutine(AnimateWalk(direction));
    }

    private void StopWalkRoutine()
    {
        if (walkRoutine != null)
        {
            StopCoroutine(walkRoutine);
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
}
