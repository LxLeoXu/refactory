using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Boss s vlastnym HP, dvoma fazami a citatelnymi varovaniami.
// Nepotrebuje Enemy.cs a funguje so starymi aj Factory Weapon projektilmi.
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class GJForgeHeartBoss : MonoBehaviour, IDamageable
{
    [Header("Boss HP")]
    [Min(1f)] public float maxHealth = 220f;
    [Min(1f)] public float defaultProjectileDamage = 10f;

    [Header("Pohyb a kontakt")]
    [Min(0f)] public float moveSpeed = 1.5f;
    [Min(1f)] public float preferredDistance = 4f;
    [Min(0f)] public float contactDamage = 11f;
    [Min(0.1f)] public float contactCooldown = 1.1f;

    [Header("Utocenie")]
    [Min(0.1f)] public float attackCooldown = 3.0f;
    [Min(0.1f)] public float warningTime = 0.85f;
    [Min(0.1f)] public float bulletSpeed = 5.7f;
    [Min(0.1f)] public float bulletDamage = 8f;
    [Min(0.2f)] public float shockwaveRadius = 3.2f;
    [Min(0f)] public float shockwaveDamage = 13f;
    [Min(1f)] public float engageRange = 14f;

    [Header("UI")]
    public bool showBossHealthBar = true;

    enum Attack { TripleShot, BulletRing, Shockwave }
    Attack pendingAttack;
    float health;
    float nextAttack;
    float resolveAt;
    float nextContact;
    float lastTargetSearch;
    int attackNumber;
    bool warning;
    bool phaseTwo;
    bool defeated;
    Rigidbody2D rb;
    Transform player;
    PlayerStats playerStats;
    SpriteRenderer sprite;
    Color normalColor;
    SpriteRenderer warningCircle;
    readonly HashSet<int> handledProjectiles = new HashSet<int>();

    public float HealthFraction { get { return Mathf.Clamp01(health / Mathf.Max(1f, maxHealth)); } }

    void Awake()
    {
        health = Mathf.Max(1f, maxHealth);
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) normalColor = sprite.color;
        nextAttack = Time.time + 1.7f;
    }

    void Start() { FindPlayer(); }

    void FindPlayer()
    {
        lastTargetSearch = Time.time;
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found == null) return;
        player = found.transform;
        playerStats = found.GetComponent<PlayerStats>();
    }

    void FixedUpdate()
    {
        if (defeated || player == null || (playerStats != null && playerStats.IsDead) || warning)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 vector = (Vector2)player.position - rb.position;
        float distance = vector.magnitude;
        Vector2 direction = distance > 0.01f ? vector / distance : Vector2.zero;
        float delta = distance - preferredDistance;
        rb.linearVelocity = Mathf.Abs(delta) < 0.8f ? Vector2.zero
            : direction * (delta > 0f ? moveSpeed : -moveSpeed);
    }

    void Update()
    {
        if (defeated) return;
        if (player == null && Time.time > lastTargetSearch + 1f) FindPlayer();
        if (player == null || (playerStats != null && playerStats.IsDead))
        {
            FinishWarning();
            return;
        }

        if (warning)
        {
            if (sprite != null)
                sprite.color = (Mathf.FloorToInt(Time.time * 9f) % 2 == 0) ? Color.white : normalColor;
            if (warningCircle != null)
                warningCircle.color = (Mathf.FloorToInt(Time.time * 9f) % 2 == 0)
                    ? new Color(1f, 0.25f, 0.1f, 0.75f) : new Color(1f, 0.85f, 0.25f, 0.9f);
            if (Time.time >= resolveAt)
            {
                ExecuteAttack();
                FinishWarning();
                nextAttack = Time.time + attackCooldown * (phaseTwo ? 0.67f : 1f);
            }
            return;
        }

        if (Time.time < nextAttack || Vector2.Distance(transform.position, player.position) > engageRange) return;
        int pattern = attackNumber++ % (phaseTwo ? 3 : 2);
        pendingAttack = phaseTwo
            ? (pattern == 0 ? Attack.TripleShot : pattern == 1 ? Attack.BulletRing : Attack.Shockwave)
            : (pattern == 0 ? Attack.TripleShot : Attack.Shockwave);
        warning = true;
        resolveAt = Time.time + warningTime * (phaseTwo ? 0.8f : 1f);
        if (pendingAttack == Attack.Shockwave) ShowShockwaveWarning();
    }

    void ExecuteAttack()
    {
        if (player == null) return;
        Vector2 origin = transform.position;
        Vector2 aiming = ((Vector2)player.position - origin).normalized;
        if (aiming.sqrMagnitude < 0.01f) aiming = Vector2.up;
        Transform bulletParent = transform.parent; // v izbe, nie ako dieta pohybujuceho sa bossa

        if (pendingAttack == Attack.TripleShot)
        {
            for (int i = -1; i <= 1; i++)
            {
                Vector2 dir = Quaternion.Euler(0f, 0f, i * (phaseTwo ? 22f : 16f)) * aiming;
                GJForgeHeartBullet.Spawn(origin + aiming * 0.75f, dir,
                    bulletSpeed * (phaseTwo ? 1.15f : 1f), bulletDamage, 4f, bulletParent);
            }
        }
        else if (pendingAttack == Attack.BulletRing)
        {
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count + Time.time * 0.3f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                GJForgeHeartBullet.Spawn(origin + dir * 0.85f, dir,
                    bulletSpeed * 0.75f, bulletDamage * 0.8f, 4f, bulletParent);
            }
        }
        else if (pendingAttack == Attack.Shockwave)
        {
            if (Vector2.Distance(origin, player.position) <= shockwaveRadius &&
                playerStats != null && !playerStats.IsDead)
                playerStats.TakeDamage(shockwaveDamage,
                    ((Vector2)player.position - origin).normalized);
        }
    }

    void ShowShockwaveWarning()
    {
        if (warningCircle == null)
        {
            GameObject ring = new GameObject("Shockwave_Warning");
            ring.transform.SetParent(transform, false);
            warningCircle = ring.AddComponent<SpriteRenderer>();
            warningCircle.sprite = GJForgeHeartBullet.WarningRingSprite;
            warningCircle.sortingOrder = 8;
        }
        float scale = Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
        warningCircle.transform.localScale = Vector3.one * (shockwaveRadius * 2f / scale);
        warningCircle.gameObject.SetActive(true);
    }

    void FinishWarning()
    {
        warning = false;
        if (sprite != null) sprite.color = normalColor;
        if (warningCircle != null) warningCircle.gameObject.SetActive(false);
    }

    public void TakeDamage(float damage, Vector2 hitDirection)
    {
        if (defeated || damage <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        if (!phaseTwo && health <= maxHealth * 0.5f)
        {
            phaseTwo = true;
            normalColor = new Color(1f, 0.55f, 0.42f, 1f);
            if (sprite != null && !warning) sprite.color = normalColor;
            nextAttack = Mathf.Min(nextAttack, Time.time + 0.7f);
            Debug.Log("FORGE HEART: Phase 2!");
        }
        if (health > 0f) return;
        defeated = true;
        // Volitelny loot, ak na prefab neskor pridas EnemyLoot.
        Component loot = GetComponent("EnemyLoot");
        if (loot != null) loot.SendMessage("Drop", SendMessageOptions.DontRequireReceiver);
        Destroy(gameObject); // BossSelector a Room cakaju na zmiznutie bossa
    }

    void OnTriggerEnter2D(Collider2D other) { ProcessProjectile(other); }
    void OnCollisionEnter2D(Collision2D collision) { ProcessProjectile(collision.collider); }

    void ProcessProjectile(Collider2D col)
    {
        if (defeated || col == null || !col.CompareTag("Projectile")) return;
        GameObject projectile = col.attachedRigidbody != null ? col.attachedRigidbody.gameObject : col.gameObject;
        // FactoryWeaponHit uz sam vola IDamageable.TakeDamage - NEPOSKODZOVAT 2x.
        if (projectile.GetComponent("FactoryWeaponHit") != null ||
            col.GetComponent("FactoryWeaponHit") != null) return;
        if (!handledProjectiles.Add(projectile.GetInstanceID())) return;
        TakeDamage(ReadProjectileDamage(projectile),
            ((Vector2)transform.position - (Vector2)projectile.transform.position).normalized);
        Destroy(projectile);
    }

    float ReadProjectileDamage(GameObject projectile)
    {
        Component component = projectile.GetComponent("Projectile");
        if (component == null) return defaultProjectileDamage;
        Type type = component.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        PropertyInfo property = type.GetProperty("Damage", flags);
        if (property != null && property.PropertyType == typeof(float))
            return Mathf.Max(0f, (float)property.GetValue(component, null));
        FieldInfo field = type.GetField("Damage", flags);
        if (field != null && field.FieldType == typeof(float))
            return Mathf.Max(0f, (float)field.GetValue(component));
        return defaultProjectileDamage;
    }

    void OnCollisionStay2D(Collision2D collision) { Contact(collision.collider); }
    void OnTriggerStay2D(Collider2D other) { Contact(other); }

    void Contact(Collider2D other)
    {
        if (defeated || other == null || Time.time < nextContact) return;
        PlayerStats stats = other.GetComponentInParent<PlayerStats>();
        if (stats == null || stats.IsDead) return;
        stats.TakeDamage(contactDamage,
            ((Vector2)stats.transform.position - (Vector2)transform.position).normalized);
        nextContact = Time.time + contactCooldown;
    }

    void OnGUI()
    {
        if (defeated || !showBossHealthBar) return;
        float w = Mathf.Min(420f, Screen.width - 30f);
        float x = (Screen.width - w) * 0.5f;
        GUI.Box(new Rect(x, 16f, w, 54f), "FORGE HEART  -  PHASE " + (phaseTwo ? "2" : "1"));
        GUI.Box(new Rect(x + 12f, 44f, (w - 24f) * HealthFraction, 15f), GUIContent.none);
        GUI.Label(new Rect(x + 15f, 41f, w - 30f, 20f),
            Mathf.CeilToInt(health) + " / " + Mathf.CeilToInt(maxHealth) + " HP");
    }
}
