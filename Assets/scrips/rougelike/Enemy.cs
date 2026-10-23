using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Zdravie")]
    public float maxHealth = 30f;
    public float projectileDamage = 10f;

    [Header("Pohyb")]
    public float moveSpeed = 2.5f;

    [Header("Kontakt s hracom")]
    public float contactDamage = 10f;
    public float attackCooldown = 1f;

    float health;
    float nextAttackTime;
    Rigidbody2D rb;
    Transform player;
    PlayerStats playerStats;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        health = maxHealth;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerStats = p.GetComponent<PlayerStats>();
        }
    }

    void FixedUpdate()
    {
        if (player == null || (playerStats != null && playerStats.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;
        rb.linearVelocity = direction * moveSpeed;
    }

    // Ak ma nepriatel bezny (netrigger) collider.
    void OnCollisionStay2D(Collision2D collision)
    {
        TryAttack(collision.collider);
    }

    // Ak ma nepriatel trigger collider.
    void OnTriggerStay2D(Collider2D other)
    {
        TryAttack(other);
    }

    void TryAttack(Collider2D other)
    {
        if (Time.time < nextAttackTime || !other.CompareTag("Player")) return;

        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;

        Vector2 direction = ((Vector2)other.transform.position - rb.position).normalized;
        stats.TakeDamage(contactDamage, direction);
        nextAttackTime = Time.time + attackCooldown;
    }

    // Zachova povodne fungovanie projektilov oznacenych tagom Projectile.
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Projectile")) return;
        TakeDamage(projectileDamage, (Vector2)(transform.position - other.transform.position));
        Destroy(other.gameObject);
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (health <= 0f || damage <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        if (health <= 0f)
        {
            EnemyLoot loot = GetComponent<EnemyLoot>();
            if (loot != null) loot.Drop();
            Destroy(gameObject);
        }
    }
}
