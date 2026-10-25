using UnityEngine;

// Jednoducha pasca na podlahe. Nepouziva Input System ani cudzie skripty.
public class DungeonSpikeTrap : MonoBehaviour
{
    [Min(0f)] public float damage = 7f;
    [Min(0.1f)] public float hitCooldown = 1.25f;
    float nextHitTime;

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || Time.time < nextHitTime) return;
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;
        nextHitTime = Time.time + hitCooldown;
        Vector2 direction = (Vector2)(other.transform.position - transform.position);
        stats.TakeDamage(damage, direction.normalized);
    }
}
