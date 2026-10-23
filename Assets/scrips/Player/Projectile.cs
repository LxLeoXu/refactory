using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 velocity;

    public void Init(Vector2 direction, float speed, Collider2D ownerCollider)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        velocity = direction.normalized * speed;
        rb.linearVelocity = velocity;

        // projektil nesmie kolidovať s tým, kto ho vystrelil
        if (ownerCollider != null)
        {
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(c, ownerCollider);
        }
    }

    // každý fyzikálny krok vynúti konštantnú rýchlosť, nič ho nespomalí ani nezastaví
    void FixedUpdate()
    {
        if (rb != null)
            rb.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ignoruj hráča, iné projektily a triggery (napr. trigger miestnosti)
        if (other.CompareTag("Player") || other.CompareTag("Projectile") || other.isTrigger)
            return;

        // narazil do steny, dverí alebo nepriateľa
        Destroy(gameObject);
    }
}
