using UnityEngine;

public enum EnemyBehavior { Chaser, Runner, Shooter, Tank, Boss }

// Jeden skript pre vsetkych nepriatelov. Rozdiely nastavuj na prefabe.
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Typ nepriatela")]
    public EnemyBehavior behavior = EnemyBehavior.Chaser;

    [Header("Statistiky")]
    public float maxHealth = 25f;
    [Tooltip("Kolko HP uberie jeden hracov projektil (zatial bez damage statov zbrane).")]
    public float projectileDamage = 10f;
    public float moveSpeed = 2.8f;
    public float contactDamage = 8f;
    public float attackCooldown = 1f;

    [Header("Runner - vystrahne sa a vyrazi")]
    public float runnerTriggerDistance = 7f;
    public float dashWindup = 0.4f;
    public float dashSpeed = 9f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 2.3f;

    [Header("Shooter / Boss - strely")]
    public float preferredDistance = 5f;
    public float shootRange = 9f;
    public float shootCooldown = 1.5f;
    public float bulletSpeed = 7f;
    public float bulletDamage = 7f;
    public float bulletLifetime = 3f;

    Rigidbody2D rb;
    Transform player;
    PlayerStats playerStats;
    SpriteRenderer sprite;
    Color originalColor;
    float health;
    float nextContactTime;
    float nextShootTime;
    float nextDashTime;
    float stateUntil;
    Vector2 dashDirection;

    enum RunnerState { Follow, Warning, Dashing }
    RunnerState runnerState = RunnerState.Follow;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        health = maxHealth;
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) originalColor = sprite.color;
    }

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            playerStats = p.GetComponent<PlayerStats>();
        }
        nextShootTime = Time.time + Random.Range(0.5f, shootCooldown + 0.5f);
        nextDashTime = Time.time + Random.Range(0.5f, 1.5f);
    }

    void FixedUpdate()
    {
        if (player == null || (playerStats != null && playerStats.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.001f ? toPlayer / distance : Vector2.zero;

        switch (behavior)
        {
            case EnemyBehavior.Runner:
                MoveRunner(direction, distance);
                break;
            case EnemyBehavior.Shooter:
            case EnemyBehavior.Boss:
                MoveShooter(direction, distance);
                TryShoot(direction, distance);
                break;
            default: // Chaser a Tank idu jednoducho za hracom
                rb.linearVelocity = direction * moveSpeed;
                break;
        }
    }

    void MoveRunner(Vector2 direction, float distance)
    {
        if (runnerState == RunnerState.Warning)
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time < stateUntil) return;

            runnerState = RunnerState.Dashing;
            stateUntil = Time.time + Mathf.Max(0.05f, dashDuration);
            if (sprite != null) sprite.color = originalColor;
        }

        if (runnerState == RunnerState.Dashing)
        {
            if (Time.time < stateUntil)
            {
                rb.linearVelocity = dashDirection * dashSpeed;
                return;
            }
            runnerState = RunnerState.Follow;
            nextDashTime = Time.time + Mathf.Max(0.2f, dashCooldown);
        }

        if (distance <= runnerTriggerDistance && Time.time >= nextDashTime)
        {
            dashDirection = direction;
            runnerState = RunnerState.Warning;
            stateUntil = Time.time + Mathf.Max(0.1f, dashWindup);
            rb.linearVelocity = Vector2.zero;
            if (sprite != null) sprite.color = Color.Lerp(originalColor, Color.red, 0.65f);
            return;
        }

        rb.linearVelocity = direction * moveSpeed;
    }

    void MoveShooter(Vector2 direction, float distance)
    {
        float difference = distance - preferredDistance;
        if (difference > 0.75f)
            rb.linearVelocity = direction * moveSpeed;
        else if (difference < -0.75f)
            rb.linearVelocity = -direction * moveSpeed;
        else
            rb.linearVelocity = Vector2.zero;
    }

    void TryShoot(Vector2 direction, float distance)
    {
        if (distance > shootRange || Time.time < nextShootTime || direction == Vector2.zero)
            return;

        nextShootTime = Time.time + Mathf.Max(0.25f, shootCooldown);
        Vector2 muzzle = rb.position + direction * 0.75f;
        if (behavior == EnemyBehavior.Boss)
        {
            // Boss vystreli tri lahke projektily do vejara.
            EnemyBullet.Fire(muzzle, Rotate(direction, -18f), bulletSpeed, bulletDamage, bulletLifetime);
            EnemyBullet.Fire(muzzle, direction, bulletSpeed, bulletDamage, bulletLifetime);
            EnemyBullet.Fire(muzzle, Rotate(direction, 18f), bulletSpeed, bulletDamage, bulletLifetime);
        }
        else
            EnemyBullet.Fire(muzzle, direction, bulletSpeed, bulletDamage, bulletLifetime);
    }

    static Vector2 Rotate(Vector2 v, float angle)
    {
        return Quaternion.Euler(0f, 0f, angle) * v;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryContactDamage(collision.collider);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryContactDamage(other);
    }

    void TryContactDamage(Collider2D other)
    {
        if (Time.time < nextContactTime) return;
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;

        Vector2 direction = ((Vector2)other.transform.position - rb.position).normalized;
        stats.TakeDamage(contactDamage, direction);
        nextContactTime = Time.time + Mathf.Max(0.1f, attackCooldown);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Projectile")) return;

        Projectile bullet = other.GetComponentInParent<Projectile>();
        float hitDamage = bullet != null ? bullet.Damage : projectileDamage;
        Vector2 hitDirection = (Vector2)transform.position - (Vector2)other.transform.position;
        TakeDamage(hitDamage, hitDirection);

        if (bullet != null) Destroy(bullet.gameObject);
        else Destroy(other.gameObject);
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (health <= 0f || damage <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        if (health > 0f) return;

        // EnemyLoot je volitelny; balicek funguje aj bez loot skriptu.
        Component loot = GetComponent("EnemyLoot");
        if (loot != null) loot.SendMessage("Drop", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject);
    }
}
