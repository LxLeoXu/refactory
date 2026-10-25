using UnityEngine;

// Spawned automatically by GJFieldTurretDeployer. Do NOT add to Player or to a prefab.
[DisallowMultipleComponent]
public class GJFieldTurret : MonoBehaviour
{
    Room room;
    Transform owner;
    float expiryTime;
    float range;
    float interval;
    float damage;
    float bulletSpeed;
    float nextFire;
    Transform barrel;

    static Sprite squareSprite;
    static Sprite WhiteSprite
    {
        get
        {
            if (squareSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                squareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }
            return squareSprite;
        }
    }

    public void Initialize(Room parentRoom, Transform player, float lifetime,
        float detectionRange, float fireEvery, float shotDamage, float shotSpeed)
    {
        room = parentRoom;
        owner = player;
        expiryTime = Time.time + Mathf.Max(1f, lifetime);
        range = Mathf.Max(1f, detectionRange);
        interval = Mathf.Max(0.1f, fireEvery);
        damage = Mathf.Max(0.1f, shotDamage);
        bulletSpeed = Mathf.Max(0.1f, shotSpeed);
        nextFire = Time.time + 0.3f;
        CreateVisuals();
    }

    void CreateVisuals()
    {
        MakePart("Base", transform, new Vector2(0f, -0.14f),
            new Vector2(0.8f, 0.26f), new Color(0.18f, 0.21f, 0.29f), 9);
        MakePart("Base_Light", transform, new Vector2(0f, -0.14f),
            new Vector2(0.55f, 0.10f), new Color(1f, 0.66f, 0.19f), 10);
        MakePart("Body", transform, new Vector2(0f, 0.10f),
            new Vector2(0.52f, 0.48f), new Color(0.27f, 0.34f, 0.42f), 10);
        MakePart("Core", transform, new Vector2(0f, 0.10f),
            new Vector2(0.20f, 0.20f), new Color(0.25f, 1f, 0.94f), 12);
        GameObject pivot = new GameObject("Barrel_Pivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        barrel = pivot.transform;
        MakePart("Barrel", barrel, new Vector2(0.32f, 0f),
            new Vector2(0.56f, 0.18f), new Color(0.80f, 0.88f, 0.97f), 11);
        MakePart("Muzzle", barrel, new Vector2(0.60f, 0f),
            new Vector2(0.13f, 0.25f), new Color(1f, 0.69f, 0.17f), 12);
    }

    static void MakePart(string partName, Transform parent, Vector2 localPosition,
        Vector2 localScale, Color tint, int sortingOrder)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite;
        renderer.color = tint;
        renderer.sortingOrder = sortingOrder;
    }

    void Update()
    {
        if (Time.time >= expiryTime || room == null || owner == null)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.time < nextFire) return;

        PlayerStats stats = owner.GetComponent<PlayerStats>();
        if (stats != null && stats.IsDead) return;

        // The turret only fights enemies in its own room.
        Collider2D[] possibleTargets = Physics2D.OverlapCircleAll(transform.position, range);
        IDamageable closest = null;
        Transform closestTransform = null;
        float bestDistance = range * range;
        foreach (Collider2D collider in possibleTargets)
        {
            if (collider == null) continue;
            IDamageable candidate = collider.GetComponentInParent<IDamageable>();
            if (candidate == null || candidate is PlayerStats) continue;
            Component component = candidate as Component;
            if (component == null || !component.gameObject.activeInHierarchy ||
                component.GetComponentInParent<Room>() != room) continue;

            Vector2 delta = (Vector2)component.transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude >= bestDistance ||
                !HasLineOfSight(component.transform)) continue;
            bestDistance = delta.sqrMagnitude;
            closest = candidate;
            closestTransform = component.transform;
        }

        if (closest == null || closestTransform == null) return;
        Vector2 direction = ((Vector2)closestTransform.position - (Vector2)transform.position).normalized;
        if (direction.sqrMagnitude < 0.01f) direction = Vector2.right;
        if (barrel != null) barrel.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        Vector2 muzzle = (Vector2)transform.position + Vector2.up * 0.15f + direction * 0.65f;
        GJFieldTurretBullet.Spawn(muzzle, direction, room,
            damage, bulletSpeed, 2f);
        nextFire = Time.time + interval;
    }

    bool HasLineOfSight(Transform target)
    {
        Vector2 delta = (Vector2)target.position - (Vector2)transform.position;
        float distance = delta.magnitude;
        if (distance < 0.1f) return true;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, delta / distance, distance);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D c = hit.collider;
            if (c == null) continue;
            IDamageable potential = c.GetComponentInParent<IDamageable>();
            if (potential is PlayerStats) continue;
            if (potential != null) return (potential as Component).transform == target;
            if (c.isTrigger) continue; // Room boundary trigger, pickups, traps
            return false; // Actual wall/crate between turret and enemy
        }
        return true;
    }
}
