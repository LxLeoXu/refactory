using UnityEngine;

// Apply damage exactly once through IDamageable, independent of the Enemy.cs variant.
// Works with enemies whose Collider2D is a trigger or a normal collider.
public class FactoryWeaponHit : MonoBehaviour
{
    float damage = 10f;
    Transform owner;
    bool done;

    public void Init(float value, Transform player)
    {
        damage = Mathf.Max(0f, value);
        owner = player;
    }

    void OnTriggerEnter2D(Collider2D other) { Handle(other); }
    void OnCollisionEnter2D(Collision2D collision) { Handle(collision.collider); }

    void Handle(Collider2D other)
    {
        if (done || other == null) return;
        if (other.transform == owner || (owner != null && other.transform.IsChildOf(owner))) return;
        if (other.CompareTag("Player") || other.CompareTag("Projectile")) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target is PlayerStats) return;
        if (target != null)
        {
            done = true;
            Vector2 direction = (Vector2)(other.transform.position - transform.position);
            target.TakeDamage(damage, direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right);
            Destroy(gameObject);
            return;
        }
        // Ignore room entry triggers; let ordinary walls destroy the projectile.
        if (!other.isTrigger)
        {
            done = true;
            Destroy(gameObject);
        }
    }
}
