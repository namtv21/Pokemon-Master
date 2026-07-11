using UnityEngine;

/// <summary>
/// Hiệu ứng "rẽ cỏ": các mảnh lá tách sang HAI BÊN + bắn lên khi bước trong cỏ cao,
/// có xoay và trọng lực giả — tạo cảm giác cỏ bị gạt sang hai phía.
/// Sprite sinh bằng code (không cần asset), mỗi lá tự animate rồi tự hủy.
/// </summary>
public class GrassLeafFx : MonoBehaviour
{
    private const float LifeTime = 0.45f;

    private static Sprite leafSprite;

    private SpriteRenderer sr;
    private Vector3 velocity;
    private float spinSpeed;
    private float age;
    private float baseScale;

    /// Gọi mỗi bước chân trong cỏ — bắn 6 lá tách đều sang hai bên chân.
    public static void Spawn(Vector3 feetPosition)
    {
        for (int i = 0; i < 6; i++)
        {
            bool leftSide = i < 3;   // 3 lá trái, 3 lá phải → cảm giác cỏ "rẽ" hai bên

            var go = new GameObject("GrassLeaf");
            go.transform.position = feetPosition + new Vector3(
                Random.Range(0.02f, 0.15f) * (leftSide ? -1f : 1f),
                Random.Range(-0.05f, 0.1f), 0f);

            var fx = go.AddComponent<GrassLeafFx>();
            fx.sr = go.AddComponent<SpriteRenderer>();
            fx.sr.sprite = GetLeafSprite();
            // các sắc xanh lá hơi khác nhau cho tự nhiên
            fx.sr.color = new Color(0.2f + Random.value * 0.2f, 0.6f + Random.value * 0.3f, 0.25f, 1f);
            fx.sr.sortingOrder = 20;   // nổi trên nền cỏ

            // tách ngang sang bên mình thuộc về + hất lên, rơi dần
            float sideSpeed = Random.Range(1f, 2.2f) * (leftSide ? -1f : 1f);
            fx.velocity = new Vector3(sideSpeed, Random.Range(1.4f, 2.4f), 0f);
            fx.spinSpeed = Random.Range(-540f, 540f);
            fx.baseScale = Random.Range(0.9f, 1.4f);
            go.transform.localScale = Vector3.one * fx.baseScale;
        }
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= LifeTime)
        {
            Destroy(gameObject);
            return;
        }

        velocity += Vector3.down * (8f * Time.deltaTime);            // trọng lực
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);        // xoay lá

        float p = age / LifeTime;
        var c = sr.color;
        c.a = 1f - p * p;                                            // mờ dần về cuối
        sr.color = c;
        transform.localScale = Vector3.one * (baseScale * (1f - 0.3f * p));
    }

    private static Sprite GetLeafSprite()
    {
        if (leafSprite != null)
            return leafSprite;

        // 8x6 chữ nhật → xoay lên trông giống lá/cọng cỏ hơn là chấm vuông
        var tex = new Texture2D(8, 6, TextureFormat.RGBA32, false);
        var pixels = new Color[8 * 6];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;   // khớp phong cách pixel
        leafSprite = Sprite.Create(tex, new Rect(0, 0, 8, 6), new Vector2(0.5f, 0.5f), 32f);
        return leafSprite;
    }
}
