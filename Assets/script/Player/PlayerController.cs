using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    [Header("Dialog")]
    [SerializeField] private Sprite portrait;

    public float moveSpeed = 3f; 
    public LayerMask SolidObjectsLayer;
    public LayerMask GrassLayer;
    public LayerMask InteractableLayer;
    public Animator animator;
    private Rigidbody2D rb; // rigidbody của người chơi
    private Vector2 input; // vector input người chơi
    private Vector3 targetPos; // vector vị trí mục tiêu
    public bool isMoving;
    private float cellSize = 1f;
    private CoroutineHost coroutineHost;

    //Hàm khởi tạo
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        RebindAnimator();
        coroutineHost = CoroutineHost.GetOrCreate();

        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindAnimator();
    }

    private void RebindAnimator()
    {
        if (animator != null)
            return;

        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);
    }

    //Hàm update sau mỗi frame
    public void HandleUpdate()
    {
        if (isMoving) return;
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;
        if (GameController.Instance.State != GameState.Overworld) return;

        if (animator == null)
        {
            RebindAnimator();
            if (animator == null) return;
        }

        input = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // tránh di chuyển chéo
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y)) input.y = 0;
        else input.x = 0;

        if (input != Vector2.zero)
        {
            animator.SetFloat("Horizontal", input.x);
            animator.SetFloat("Vertical", input.y);
            animator.SetFloat("Speed", 1f);

            var newTarget = rb.position + input * cellSize;
            if (IsWalkable(newTarget))
            {
                targetPos = new Vector3(newTarget.x, newTarget.y, 0);
                StartMoveRoutine(targetPos);
            }
            else
            {
                animator.SetFloat("Speed", 0f);
            }
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var dialogManager = DialogManager.Instance;
            if (dialogManager != null && dialogManager.IsDebouncingInput)
                return;

            Interact();
        }
        
    }

    //Hàm tương tác
    void Interact()
    {
         Vector2 facingDir = new Vector2(
                animator.GetFloat("Horizontal"),
                animator.GetFloat("Vertical")
            );
            Vector2 interactPos = rb.position + facingDir * cellSize;

            var hit = Physics2D.OverlapCircle(interactPos, 0.3f, InteractableLayer);
            if (hit != null)
            {
                hit.GetComponent<Interactable>()?.Interact();
            }
    }

    public Vector2 GetFacingDirection()
    {
        if (animator == null)
            return Vector2.down;

        var facing = new Vector2(animator.GetFloat("Horizontal"), animator.GetFloat("Vertical"));
        if (facing == Vector2.zero)
            return Vector2.down;

        return facing.normalized;
    }

    public Vector2 GetPosition()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }

    public Sprite Portrait => portrait;

    public void SetPortrait(Sprite newPortrait)
    {
        portrait = newPortrait;
    }

    //Hàm di chuyển
    System.Collections.IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        Vector2 target2D = target;

        while ((target2D - rb.position).sqrMagnitude > 0.001f)
        {
            var nextPos = Vector2.MoveTowards(rb.position, target2D, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(nextPos);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(target2D);
        isMoving = false;
        
        foreach (var grassTrigger in FindObjectsOfType<GrassTrigger>())
        {
            grassTrigger.TryEncounter();
        }
        

    }

    private void StartMoveRoutine(Vector3 target)
    {
        if (coroutineHost == null)
            coroutineHost = CoroutineHost.GetOrCreate();

        if (coroutineHost == null)
        {
            Debug.LogWarning("[PlayerController] Coroutine host is unavailable.");
            return;
        }

        coroutineHost.StartCoroutine(MoveTo(target));
    }

    // Kiểm tra va chạm
    bool IsWalkable(Vector2 target)
    {
        var hit = Physics2D.OverlapCircle(target, 0.2f, SolidObjectsLayer | InteractableLayer);
        return hit == null;
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

            var go = new GameObject("PlayerControllerRunner");
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