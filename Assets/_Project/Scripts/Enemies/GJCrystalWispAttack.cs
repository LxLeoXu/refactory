using UnityEngine;

// Specialny utok Wisp nepriatela. Nezasahuje do existujuceho Enemy.cs.
public class GJCrystalWispAttack : MonoBehaviour
{
    [Header("Crystal Wisp - kruhova salva")]
    [Min(2)] public int bulletCount = 6;
    [Min(0f)] public float bulletDamage = 5f;
    [Min(0.2f)] public float bulletSpeed = 4.2f;
    [Min(0.2f)] public float bulletLifetime = 3.5f;
    [Min(0.2f)] public float attackCooldown = 4.5f;
    [Min(0.1f)] public float warningTime = 0.7f;
    [Min(1f)] public float detectionRange = 8f;

    Transform player;
    PlayerStats playerStats;
    SpriteRenderer sprite;
    Color normalColor;
    float nextAttack;
    float fireAt;
    bool warning;

    void Awake()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) normalColor = sprite.color;
    }

    void Start()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Player");
        if (target != null)
        {
            player = target.transform;
            playerStats = target.GetComponent<PlayerStats>();
        }
        nextAttack = Time.time + Random.Range(1.5f, 3f);
    }

    void Update()
    {
        if (player == null || (playerStats != null && playerStats.IsDead))
        {
            if (warning) EndWarning();
            return;
        }

        if (warning)
        {
            if (sprite != null)
                sprite.color = Color.Lerp(normalColor, Color.white,
                    Mathf.PingPong(Time.time * 7f, 1f) * 0.8f);
            if (Time.time >= fireAt)
            {
                FireRing();
                EndWarning();
                nextAttack = Time.time + Mathf.Max(0.2f, attackCooldown);
            }
            return;
        }

        if (Time.time < nextAttack) return;
        if (Vector2.Distance(transform.position, player.position) > detectionRange) return;

        warning = true;
        fireAt = Time.time + Mathf.Max(0.1f, warningTime);
    }

    void FireRing()
    {
        Vector2 origin = transform.position;
        int count = Mathf.Max(2, bulletCount);
        float offset = Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float radians = (offset + i * 360f / count) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            GJCrystalWispBolt.Spawn(origin, direction, bulletSpeed, bulletDamage,
                bulletLifetime, transform.parent);
        }
    }

    void EndWarning()
    {
        warning = false;
        if (sprite != null) sprite.color = normalColor;
    }

    void OnDisable()
    {
        if (sprite != null) sprite.color = normalColor;
    }
}
