using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [System.Serializable]
    private class IntroScreen
    {
        [TextArea(3, 6)] public string text;
        public float autoAdvanceDelay;
        public bool requireZToAdvance;
    }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI introText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string nextSceneName = "Prologue";
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private IntroScreen[] introScreens = new IntroScreen[]
    {
        new IntroScreen
        {
            text = "Dự án fan-made này được tạo ra cho mục đích học tập và thực hành phát triển game.\n\nTrò chơi không có liên hệ, tài trợ, hay xác nhận chính thức từ Nintendo, Game Freak, Creatures Inc., The Pokémon Company, hoặc bất kỳ đơn vị sở hữu bản quyền nào.\n\nPokémon, tên riêng, nhân vật và tài sản liên quan thuộc quyền sở hữu của chủ sở hữu tương ứng.",
            autoAdvanceDelay = 3f,
            requireZToAdvance = false
        },
        new IntroScreen
        {
            text = "[Tự viết màn 2: giới thiệu cách di chuyển, tương tác, hoặc mục tiêu chính của game]",
            autoAdvanceDelay = 0f,
            requireZToAdvance = true
        },
        new IntroScreen
        {
            text = "[Tự viết màn 3: hướng dẫn ngắn về cách bắt đầu chơi, ví dụ: nhấn Z để nói chuyện, dùng phím mũi tên để di chuyển, v.v.]",
            autoAdvanceDelay = 0f,
            requireZToAdvance = true
        }
    };

    private void Start()
    {
        if (introText == null)
        {
            Debug.LogError("IntroText (TextMeshProUGUI) not assigned!");
            LoadNextScene();
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeDuration));

        for (int i = 0; i < introScreens.Length; i++)
        {
            IntroScreen screen = introScreens[i];
            introText.text = screen.text;

            if (hintText != null)
            {
                hintText.gameObject.SetActive(screen.requireZToAdvance);
                if (screen.requireZToAdvance)
                    hintText.text = "Nhấn Z để tiếp tục";
            }

            if (screen.requireZToAdvance)
            {
                while (!Input.GetKeyDown(KeyCode.Z))
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(Mathf.Max(0f, screen.autoAdvanceDelay));
            }

            yield return null;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(false);
        }

        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeDuration));
        LoadNextScene();
    }

    private IEnumerator FadeCanvas(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = endAlpha;
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
