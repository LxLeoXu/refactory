using UnityEngine;

// Varovny mechanicky nastrahovy kruh. Nevyzaduje collider ani fyzikalne efekty.
public class GJSpiderMine : MonoBehaviour
{
    static Sprite squareSprite;

    float damage;
    float triggerRadius;
    float blastRadius;
    float warningDuration;
    float expiryTime;
    float armedAt;
    float explodeAt;
    bool triggered;
    Transform player;
    PlayerStats playerStats;
    SpriteRenderer sprite;

    public static GameObject Spawn(Vector2 position, Transform roomParent,
        float damage, float triggerRadius, float blastRadius,
        float warningTime, float lifetime)
    {
        GameObject go = new GameObject("SpiderMine");
        if (roomParent != null) go.transform.SetParent(roomParent, true);
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.60f;

        if (squareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = squareSprite;
        sr.sortingOrder = 4;
        sr.color = new Color(1f, 0.6f, 0.1f);

        GJSpiderMine mine = go.AddComponent<GJSpiderMine>();
        mine.sprite = sr;
        mine.damage = damage;
        mine.triggerRadius = Mathf.Max(0.1f, triggerRadius);
        mine.blastRadius = Mathf.Max(mine.triggerRadius, blastRadius);
        mine.warningDuration = Mathf.Max(0.1f, warningTime);
        mine.armedAt = Time.time + 0.7f;
        mine.expiryTime = Time.time + Mathf.Max(1f, lifetime);

        GameObject target = GameObject.FindGameObjectWithTag("Player");
        if (target != null)
        {
            mine.player = target.transform;
            mine.playerStats = target.GetComponent<PlayerStats>();
        }
        return go;
    }

    void Update()
    {
        if (Time.time > expiryTime)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.time < armedAt)
        {
            if (sprite != null) sprite.color = Color.gray;
            return;
        }
        if (player == null || (playerStats != null && playerStats.IsDead)) return;

        float distance = Vector2.Distance(transform.position, player.position);
        if (!triggered)
        {
            if (sprite != null) sprite.color = new Color(1f, 0.6f, 0.1f);
            if (distance > triggerRadius) return;
            triggered = true;
            explodeAt = Time.time + warningDuration;
        }
        if (sprite != null)
            sprite.color = (Mathf.FloorToInt(Time.time * 12f) % 2 == 0)
                ? Color.red : Color.white;

        if (Time.time < explodeAt) return;

        // Hrac ma varovanie a moze sa vyhnut. Respektuje jeho i-frames.
        if (playerStats != null && !playerStats.IsDead && distance <= blastRadius)
        {
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            playerStats.TakeDamage(damage, direction);
        }
        Destroy(gameObject);
    }
}
