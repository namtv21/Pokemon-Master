using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HpBar : MonoBehaviour
{
    [SerializeField] private RectTransform health;      // thanh máu chính
    [SerializeField] private RectTransform background;  // nền thanh máu
    [SerializeField] private TextMeshProUGUI hp;        // text hiển thị số máu

    private static readonly Color HighColor     = new Color(0.35f, 0.85f, 0.35f); // xanh  > 50%
    private static readonly Color MidColor      = new Color(0.95f, 0.80f, 0.20f); // vàng 20-50%
    private static readonly Color LowColor      = new Color(0.90f, 0.25f, 0.20f); // đỏ   < 20%
    private static readonly Color LowFlashColor = new Color(0.55f, 0.10f, 0.08f); // đỏ sậm (nhấp nháy)

    private float currentFraction = 1f;

    private Image healthImage;
    private Image HealthImage
    {
        get
        {
            if (healthImage == null && health != null)
                healthImage = health.GetComponent<Image>();
            return healthImage;
        }
    }

    // Đặt tỉ lệ máu ngay lập tức
    public void SetHpFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        health.localScale = new Vector3(fraction, 1f, 1f);
        ApplyColor(fraction);
    }

    // Đặt số máu ngay lập tức
    public void SetHpNumbers(int currentHP, int maxHP)
    {
        if (hp != null)
            hp.text = $"{currentHP}/{maxHP}";
    }

    // Hiệu ứng giảm máu mượt (màu đổi theo trong lúc tụt)
    public IEnumerator SmoothHpChange(float targetFraction, int currentHP, int maxHP)
    {
        float startFraction = health.localScale.x;
        float t = 0f;

        while (Mathf.Abs(health.localScale.x - targetFraction) > 0.01f)
        {
            t += Time.deltaTime * 2f; // tốc độ giảm máu
            float newFraction = Mathf.Lerp(startFraction, targetFraction, t);
            health.localScale = new Vector3(newFraction, 1f, 1f);
            ApplyColor(newFraction);

            if (hp != null)
            {
                int displayedHp = Mathf.RoundToInt(newFraction * maxHP);
                hp.text = $"{displayedHp}/{maxHP}";
            }

            yield return null;
        }

        // Đảm bảo giá trị cuối cùng chính xác
        health.localScale = new Vector3(targetFraction, 1f, 1f);
        ApplyColor(targetFraction);
        if (hp != null)
            hp.text = $"{currentHP}/{maxHP}";
    }

    // Xanh → vàng → đỏ theo lượng máu (chuẩn Pokemon)
    private void ApplyColor(float fraction)
    {
        currentFraction = fraction;
        var img = HealthImage;
        if (img == null) return;

        img.color = fraction > 0.5f ? HighColor
                  : fraction > 0.2f ? MidColor
                  : LowColor;
    }

    // Máu nguy kịch → thanh đỏ nhấp nháy (tạo cảm giác căng thẳng)
    private void Update()
    {
        if (currentFraction <= 0f || currentFraction > 0.2f)
            return;

        var img = HealthImage;
        if (img == null) return;

        float t = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
        img.color = Color.Lerp(LowColor, LowFlashColor, t);
    }
}
