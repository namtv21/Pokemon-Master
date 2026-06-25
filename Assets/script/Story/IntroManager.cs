using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string nextSceneName = "Prologue";
    [SerializeField] private float fadeDuration = 0.8f;

    // ── Trang lưu ý ──────────────────────────────────────────────
    [Header("Disclaimer")]
    [SerializeField] private CanvasGroup disclaimerCanvas;
    [SerializeField] private TextMeshProUGUI disclaimerText;
    [SerializeField] private float disclaimerHoldTime = 4f;

    [TextArea(4, 10)]
    [SerializeField] private string disclaimerContent =
        "Dự án fan-made này được tạo ra cho mục đích học tập và thực hành phát triển game.\n\n" +
        "Trò chơi không có liên hệ, tài trợ hay xác nhận chính thức từ Nintendo, Game Freak, " +
        "Creatures Inc., The Pokémon Company hoặc bất kỳ đơn vị sở hữu bản quyền nào.\n\n" +
        "Pokémon, tên riêng, nhân vật và tài sản liên quan thuộc quyền sở hữu của chủ sở hữu tương ứng.";

    // ── Giới thiệu Giáo sư Oke ───────────────────────────────────
    [Header("Oak Introduction")]
    [SerializeField] private CanvasGroup oakCanvas;
    [SerializeField] private Image oakSpriteImage;
    [SerializeField] private CanvasGroup dialogPanel; // panel trắng dưới chân — optional
    [SerializeField] private TextMeshProUGUI oakDialogText;
    [SerializeField] private GameObject pressZHint;
    [SerializeField] private float typewriterDelay = 0.035f;

    [TextArea(2, 6)]
    [SerializeField] private string[] oakLines = new string[]
    {
        "Chào mừng đến với thế giới Pokémon!",
        "Thế giới này tồn tại song song với thế giới của con người...\nẩn chứa vô số sinh vật kỳ diệu chưa được khám phá.",
        "Ta là Giáo sư Oke — nhà nghiên cứu Pokémon hàng đầu vùng này.",
        "Pokémon có thể là đối tác chiến đấu, người bạn đồng hành\nvà đôi khi... là cả gia đình.",
        "Hành trình của cậu sắp bắt đầu.\nHãy cẩn thận — và chúc may mắn!"
    };

    // ─────────────────────────────────────────────────────────────

    private bool _skipTypewriter;

    private void Start()
    {
        // Đảm bảo canvas không phụ thuộc camera — tránh lỗi khi load từ scene khác
        EnsureOverlayCanvas(disclaimerCanvas);
        EnsureOverlayCanvas(oakCanvas);

        if (disclaimerCanvas != null) disclaimerCanvas.alpha = 0f;
        if (oakCanvas        != null) oakCanvas.alpha        = 0f;
        if (dialogPanel      != null) dialogPanel.alpha      = 0f;
        if (pressZHint       != null) pressZHint.SetActive(false);

        StartCoroutine(PlayIntro());
    }

    private static void EnsureOverlayCanvas(CanvasGroup group)
    {
        if (group == null) return;
        var canvas = group.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
            _skipTypewriter = true;
    }

    // ── Luồng chính ──────────────────────────────────────────────

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(PlayDisclaimer());
        yield return StartCoroutine(PlayOakIntro());

        if (oakCanvas != null)
            yield return StartCoroutine(Fade(oakCanvas, 1f, 0f));

        SceneManager.LoadScene(nextSceneName);
    }

    // ── Trang lưu ý ──────────────────────────────────────────────

    private IEnumerator PlayDisclaimer()
    {
        if (disclaimerCanvas == null) yield break;

        if (disclaimerText != null)
            disclaimerText.text = disclaimerContent;

        yield return StartCoroutine(Fade(disclaimerCanvas, 0f, 1f));
        yield return new WaitForSeconds(disclaimerHoldTime);
        yield return StartCoroutine(Fade(disclaimerCanvas, 1f, 0f));
    }

    // ── Giới thiệu Giáo sư Oke ───────────────────────────────────

    private IEnumerator PlayOakIntro()
    {
        if (oakCanvas == null) yield break;

        // Fade Oak canvas vào
        yield return StartCoroutine(Fade(oakCanvas, 0f, 1f));

        // Hiện sprite Oak (fade alpha của Image)
        if (oakSpriteImage != null)
            yield return StartCoroutine(FadeSprite(oakSpriteImage, 0f, 1f, fadeDuration));

        // Sau khi Oak hiện xong → fade dialog panel vào
        if (dialogPanel != null)
            yield return StartCoroutine(Fade(dialogPanel, 0f, 1f));

        yield return new WaitForSeconds(0.4f);

        // Từng dòng thoại
        foreach (string line in oakLines)
        {
            if (oakDialogText != null)
            {
                yield return StartCoroutine(TypewriterLine(line));

                if (pressZHint != null) pressZHint.SetActive(true);

                // Chờ Z để tiếp tục
                yield return StartCoroutine(WaitForZ());

                if (pressZHint != null) pressZHint.SetActive(false);
                yield return new WaitForSeconds(0.15f);
            }
        }

        // Oak sprite mờ dần trước khi chuyển scene
        if (oakSpriteImage != null)
            yield return StartCoroutine(FadeSprite(oakSpriteImage, 1f, 0f, fadeDuration));

        yield return new WaitForSeconds(0.3f);
    }

    // ── Hiệu ứng typewriter ──────────────────────────────────────

    private IEnumerator TypewriterLine(string line)
    {
        _skipTypewriter = false;
        oakDialogText.text = "";

        for (int i = 0; i < line.Length; i++)
        {
            if (_skipTypewriter)
            {
                oakDialogText.text = line;
                _skipTypewriter = false;
                yield break;
            }

            oakDialogText.text += line[i];

            // Dừng lâu hơn ở dấu chấm, phẩy
            char c = line[i];
            float delay = (c == '.' || c == '!' || c == '?') ? typewriterDelay * 6f
                        : (c == ',')                          ? typewriterDelay * 3f
                        : typewriterDelay;

            yield return new WaitForSeconds(delay);
        }
    }

    // ── Chờ nhấn Z (bỏ qua frame đầu để tránh Z từ typewriter skip) ──

    private IEnumerator WaitForZ()
    {
        yield return null; // bỏ qua frame Z vừa nhấn để skip typewriter
        while (!Input.GetKeyDown(KeyCode.Z))
            yield return null;
    }

    // ── Fade helpers ─────────────────────────────────────────────

    private IEnumerator Fade(CanvasGroup cg, float from, float to)
    {
        float t = 0f;
        cg.alpha = from;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
    }

    private IEnumerator FadeSprite(Image img, float from, float to, float duration)
    {
        float t = 0f;
        Color c = img.color;
        c.a = from;
        img.color = c;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}
