using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
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

     private static bool _initialized;

    //Hàm khởi tạo
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        //animator = GetComponent<Animator>();
        
        if (_initialized)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        _initialized = true;

    }

    //Hàm update sau mỗi frame
    public void HandleUpdate()
    {
        if (isMoving) return;
        if (GameController.Instance.State != GameState.Overworld) return;

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
                //StopAllCoroutines();
                StartCoroutine(MoveTo(targetPos));
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
        Interact();
        
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

    //Hàm di chuyển
    System.Collections.IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        while ((target - transform.position).sqrMagnitude > 0.001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
        isMoving = false;
        
        foreach (var grassTrigger in FindObjectsOfType<GrassTrigger>())
        {
            grassTrigger.TryEncounter();
        }
        

    }

    // Kiểm tra va chạm
    bool IsWalkable(Vector2 target)
    {
        var hit = Physics2D.OverlapCircle(target, 0.2f, SolidObjectsLayer | InteractableLayer);
        return hit == null;
    }
    
}