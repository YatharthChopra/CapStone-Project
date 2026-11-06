using UnityEngine;

// The flash when a thrown object lands: a ring that spreads out to show how far the noise carries.
// ThrownObject makes one of these. It removes itself when it has finished.
public class NoiseRing : MonoBehaviour
{
    private const float SpreadTime = 0.5f;   // seconds for the ring to grow to full size

    private SpriteRenderer ring;
    private float maxSize;
    private float timer;

    // Makes a ring at a spot. "radius" is how far the noise can be heard, in world units.
    public static void Create(Vector2 position, float radius, Sprite circleSprite)
    {
        if (circleSprite == null) return;

        GameObject ringObject = new GameObject("Noise Ring");
        ringObject.transform.position = position;

        NoiseRing noiseRing = ringObject.AddComponent<NoiseRing>();
        noiseRing.maxSize = radius * 2f;

        noiseRing.ring = ringObject.AddComponent<SpriteRenderer>();
        noiseRing.ring.sprite = circleSprite;
        noiseRing.ring.sortingOrder = 15;
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / SpreadTime);

        // Grows quickly at first, then slows down, and fades away as it spreads
        float size = maxSize * (1f - (1f - progress) * (1f - progress));
        transform.localScale = new Vector3(size, size, 1f);
        ring.color = new Color(1f, 1f, 1f, 0.35f * (1f - progress));

        if (progress >= 1f) Destroy(gameObject);
    }
}
