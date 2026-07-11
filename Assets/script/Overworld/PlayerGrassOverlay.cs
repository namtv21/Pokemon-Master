using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bụi cỏ "nửa dưới" đè lên chân player khi đứng trong cỏ cao (chuẩn GBA):
/// tilemap cỏ giữ nguyên SAU player, chỉ một sprite overlay TRƯỚC player che phần chân.
/// GrassTrigger báo vào/ra cỏ qua NotifyGrass (theo collider — chỉnh collider vùng cỏ
/// cho khớp vùng muốn tính là "trong cỏ").
///
/// - Sorting: TỰ đặt trên sprite player (đọc renderer của player + orderOffset).
/// - Vị trí: TỰ snap vào tâm ô tile player đang đứng (dùng Grid của Tilemap).
///
/// Cách dùng: gắn lên Player, kéo sprite tile cỏ (bản mất nửa trên) vào Half Grass Sprite.
/// </summary>
public class PlayerGrassOverlay : MonoBehaviour
{
    [Tooltip("Sprite tile cỏ cao bản MẤT NỬA TRÊN (chỉ còn nửa dưới) — che chân player.")]
    [SerializeField] private Sprite halfGrassSprite;

    [Header("Sorting (tự động theo player)")]
    [Tooltip("Overlay vẽ trên sprite player bao nhiêu bậc.")]
    [SerializeField] private int orderOffset = 1;

    [Header("Vị trí")]
    [Tooltip("Snap bụi cỏ vào tâm ô tile player đang đứng (khớp lưới tile). Tắt → bám theo player.")]
    [SerializeField] private bool snapToTileGrid = true;
    [Tooltip("Điểm dò 'chân' player so với pivot — chỉ dùng để xác định player đang đứng ở Ô nào.")]
    [SerializeField] private Vector2 feetProbeOffset = new Vector2(0f, -0.2f);
    [Tooltip("Tinh chỉnh vị trí bụi cỏ sau khi snap (nếu pivot sprite cỏ lệch).")]
    [SerializeField] private Vector2 overlayNudge = Vector2.zero;

    private static PlayerGrassOverlay instance;

    private SpriteRenderer overlay;
    private SpriteRenderer playerRenderer;
    private Grid grid;
    private int grassContacts;   // đếm số vùng cỏ đang chạm (vùng chồng nhau vẫn đúng)

    /// GrassTrigger gọi khi player vào/ra vùng cỏ.
    public static void NotifyGrass(bool entered)
    {
        if (instance != null)
            instance.OnGrassContact(entered);
    }

    private void Awake()
    {
        instance = this;

        // Lấy renderer của player TRƯỚC khi tạo overlay (tránh nhặt nhầm chính overlay)
        playerRenderer = GetComponentInChildren<SpriteRenderer>();

        var go = new GameObject("GrassFeetOverlay");
        go.transform.SetParent(transform, false);

        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = halfGrassSprite;
        ApplySorting();
        overlay.enabled = false;
    }

    // Luôn vẽ trên sprite player, cùng sorting layer — không phải dò số tay.
    private void ApplySorting()
    {
        if (playerRenderer != null)
        {
            overlay.sortingLayerID = playerRenderer.sortingLayerID;
            overlay.sortingOrder = playerRenderer.sortingOrder + Mathf.Max(1, orderOffset);
        }
        else
        {
            overlay.sortingOrder = 10;   // fallback an toàn
        }
    }

    private void LateUpdate()
    {
        if (overlay == null || !overlay.enabled)
            return;

        Vector3 target;

        if (snapToTileGrid && (grid != null || (grid = FindObjectOfType<Grid>()) != null))
        {
            // Ô tile chứa "chân" player → đặt bụi cỏ đúng tâm ô như một tile thật
            Vector3 feet = transform.position + (Vector3)feetProbeOffset;
            target = grid.GetCellCenterWorld(grid.WorldToCell(feet)) + (Vector3)overlayNudge;
        }
        else
        {
            // Không snap (hoặc scene không có Grid): bám theo player
            target = transform.position + (Vector3)(feetProbeOffset + overlayNudge);
        }

        // Căn theo TÂM HÌNH nhìn thấy (bounds) thay vì pivot — pivot sprite lệch hay
        // rotate 180 thì hình vẫn nằm đúng chỗ, không bị hất lên đầu nhân vật.
        overlay.transform.position = target;
        overlay.transform.position += target - overlay.bounds.center;
    }

    private void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // Đổi scene: trigger exit có thể không kịp bắn + Grid là của scene cũ → reset.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        grassContacts = 0;
        grid = null;
        if (overlay != null) overlay.enabled = false;
    }

    private void OnGrassContact(bool entered)
    {
        grassContacts = Mathf.Max(0, grassContacts + (entered ? 1 : -1));
        if (overlay == null)
            return;

        bool show = grassContacts > 0 && halfGrassSprite != null;
        if (show)
            ApplySorting();   // phòng khi order của player đổi lúc chạy
        overlay.enabled = show;
    }
}
