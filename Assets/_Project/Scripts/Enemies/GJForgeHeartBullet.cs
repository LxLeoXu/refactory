using UnityEngine;

// Jednoducha boss strela a sprite kruhoveho varovania, bez dalsich prefabov.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GJForgeHeartBullet : MonoBehaviour
{
    static Sprite bulletSprite;
    static Sprite ringSprite;
    Rigidbody2D body;
    Vector2 velocity;
    float damage;
    bool consumed;

    public static Sprite WarningRingSprite
    {
        get
        {
            if (ringSprite != null) return ringSprite;
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    pixels[y * 64 + x] = d >= 28.3f && d < 31.3f
                        ? Color.white : Color.clear;
                }
            tex.SetPixels(pixels);
            tex.Apply();
            ringSprite = Sprite.Create(tex, new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
            return ringSprite;
        }
    }

    public static void Spawn(Vector2 origin, Vector2 direction,
        float speed, float damage, float lifetime, Transform parent)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        GameObject go = new GameObject("ForgeHeart_Bullet");
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = origin;
        go.transform.localScale = new Vector3(0.32f, 0.32f, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (bulletSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            bulletSprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        sr.sprite = bulletSprite;
        sr.color = new Color(1f, 0.4f, 0.1f);
        sr.sortingOrder = 7;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.47f;

        GJForgeHeartBullet bullet = go.AddComponent<GJForgeHeartBullet>();
        bullet.body = rb;
        bullet.velocity = direction.normalized * speed;
        bullet.damage = damage;
        rb.linearVelocity = bullet.velocity;
        Destroy(go, Mathf.Max(0.2f, lifetime));
    }

    void FixedUpdate()
    {
        if (body != null) body.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || other == null || other.CompareTag("Projectile") ||
            other.GetComponentInParent<GJForgeHeartBoss>() != null ||
            other.GetComponentInParent<Enemy>() != null) return;
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            consumed = true;
            if (!stats.IsDead) stats.TakeDamage(damage, velocity.normalized);
            Destroy(gameObject);
            return;
        }
        if (other.isTrigger) return; // room trigger / pickup
        consumed = true;
        Destroy(gameObject);
    }
}
