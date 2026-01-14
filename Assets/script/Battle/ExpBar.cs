using System.Collections;
using UnityEngine;
using TMPro;

public class ExpBar : MonoBehaviour
{
    [SerializeField] private RectTransform expFill;       // thanh exp chính
    [SerializeField] private RectTransform background;    // nền thanh exp
    [SerializeField] private TextMeshProUGUI expText;     // text hiển thị số exp

    // Đặt tỉ lệ exp ngay lập tức
    public void SetExpFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        expFill.localScale = new Vector3(fraction, 1f, 1f);
    }

    // Đặt số exp ngay lập tức
    public void SetExpNumbers(int currentExp, int expToNextLevel)
    {
        if (expText != null)
            expText.text = $"{currentExp}/{expToNextLevel}";
    }

    // Hiệu ứng tăng exp mượt
    public IEnumerator SmoothExpChange(float targetFraction, int currentExp, int expToNextLevel)
    {
        float startFraction = expFill.localScale.x;
        float t = 0f;

        while (Mathf.Abs(expFill.localScale.x - targetFraction) > 0.01f)
        {
            t += Time.deltaTime * 2f; // tốc độ tăng exp
            float newFraction = Mathf.Lerp(startFraction, targetFraction, t);
            expFill.localScale = new Vector3(newFraction, 1f, 1f);

            if (expText != null)
            {
                int displayedExp = Mathf.RoundToInt(newFraction * expToNextLevel);
                expText.text = $"{displayedExp}/{expToNextLevel}";
            }

            yield return null;
        }

        // Đảm bảo giá trị cuối cùng chính xác
        expFill.localScale = new Vector3(targetFraction, 1f, 1f);
        if (expText != null)
            expText.text = $"{currentExp}/{expToNextLevel}";
    }
}
