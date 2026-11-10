using UnityEngine;

// Strela nepriatela. Nepotrebuje vlastny prefab ani graficke assety.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnemyBullet : MonoBehaviour
{
    static Sprite pixelSprite;
    Rigidbody2D body;
    Vector2 velocity;
    float damage;
    bool hit;

    public static void Fire(Vector2 origin, Vector2 direction, float speed, float damage, float lifetime)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        GameObject go = new GameObject("EnemyBullet");
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 0.35f;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        if (pixelSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            pixelSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        sr.sprite = pixelSprite;
        sr.color = new Color(1f, 0.45f, 0.22f);
        sr.sortingOrder = 5;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.48f;

        EnemyBullet bullet = go.AddComponent<EnemyBullet>();
        bullet.Init(direction, speed, damage, lifetime);
    }

    void Init(Vector2 direction, float speed, float amount, float lifetime)
    {
        body = GetComponent<Rigidbody2D>();
        damage = amount;
        velocity = direction.normalized * speed;
        body.linearVelocity = velocity;
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    void FixedUpdate()
    {
        if (body != null) body.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hit || other.GetComponentInParent<Enemy>() != null || other.CompareTag("Projectile"))
            return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            hit = true;
            stats.TakeDamage(damage, velocity.normalized);
            Destroy(gameObject);
            return;
        }

        // Ignorujeme triggery miestnosti a pickupov, ale nie pevne steny.
        if (other.isTrigger) return;
        hit = true;
        Destroy(gameObject);
    }
}
