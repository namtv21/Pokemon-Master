using System.Collections;
using UnityEngine;
using TMPro;

public class HpBar : MonoBehaviour
{
    [SerializeField] private RectTransform health;      // thanh máu chính
    [SerializeField] private RectTransform background;  // nền thanh máu
    [SerializeField] private TextMeshProUGUI hp;        // text hiển thị số máu
    // Đặt tỉ lệ máu ngay lập tức
    public void SetHpFraction(float fraction)
    {
        fraction = Mathf.Clamp01(fraction);
        health.localScale = new Vector3(fraction, 1f, 1f);
    }

    // Đặt số máu ngay lập tức
    public void SetHpNumbers(int currentHP, int maxHP)
    {
        if (hp != null)
            hp.text = $"{currentHP}/{maxHP}";
    }

    // Hiệu ứng giảm máu mượt
    public IEnumerator SmoothHpChange(float targetFraction, int currentHP, int maxHP)
    {
        float startFraction = health.localScale.x;
        float t = 0f;

        while (Mathf.Abs(health.localScale.x - targetFraction) > 0.01f)
        {
            t += Time.deltaTime * 2f; // tốc độ giảm máu
            float newFraction = Mathf.Lerp(startFraction, targetFraction, t);
            health.localScale = new Vector3(newFraction, 1f, 1f);

            if (hp != null)
            {
                int displayedHp = Mathf.RoundToInt(newFraction * maxHP);
                hp.text = $"{displayedHp}/{maxHP}";
            }

            yield return null;
        }

        // Đảm bảo giá trị cuối cùng chính xác
        health.localScale = new Vector3(targetFraction, 1f, 1f);
        if (hp != null)
            hp.text = $"{currentHP}/{maxHP}";
    }
}