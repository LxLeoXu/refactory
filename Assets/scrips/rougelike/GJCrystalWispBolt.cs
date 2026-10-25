using UnityEngine;

// Projektil Wisp nepriatela; sam si vytvori vizual, prefab netreba.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GJCrystalWispBolt : MonoBehaviour
{
    static Sprite squareSprite;
    Rigidbody2D body;
    Vector2 velocity;
    float damage;
    bool consumed;

    public static void Spawn(Vector2 position, Vector2 direction, float speed,
        float damage, float lifetime, Transform roomParent)
    {
        if (direction.sqrMagnitude < 0.0001f) return;

        GameObject go = new GameObject("CrystalWisp_Bullet");
        if (roomParent != null) go.transform.SetParent(roomParent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.28f;

        if (squareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.color = new Color(0.48f, 0.95f, 1f);
        sr.sortingOrder = 7;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.46f;

        GJCrystalWispBolt projectile = go.AddComponent<GJCrystalWispBolt>();
        projectile.damage = damage;
        projectile.velocity = direction.normalized * Mathf.Max(0.1f, speed);
        projectile.body = rb;
        rb.linearVelocity = projectile.velocity;
        Destroy(go, Mathf.Max(0.2f, lifetime));
    }

    void FixedUpdate()
    {
        if (body != null) body.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || other.GetComponentInParent<Enemy>() != null ||
            other.CompareTag("Projectile")) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            consumed = true;
            if (!stats.IsDead) stats.TakeDamage(damage, velocity.normalized);
            Destroy(gameObject);
            return;
        }

        // Ignoruj trigger miestnosti, loot a pickupov.
        if (other.isTrigger) return;

        consumed = true;
        Destroy(gameObject);
    }
}
