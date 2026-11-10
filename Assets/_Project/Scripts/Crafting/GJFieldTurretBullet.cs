using UnityEngine;

// No collider, rigidbody or Projectile tag: deals damage once via raycast.
// This avoids the duplicate-projectile-hit issue of older Enemy.cs versions.
public class GJFieldTurretBullet : MonoBehaviour
{
    static Sprite squareSprite;
    Vector2 direction;
    Room room;
    float speed;
    float damage;
    float endTime;

    public static void Spawn(Vector2 position, Vector2 shotDirection, Room targetRoom,
        float shotDamage, float shotSpeed, float lifetime)
    {
        GameObject obj = new GameObject("FieldTurret_Bullet");
        if (targetRoom != null) obj.transform.SetParent(targetRoom.transform, true);
        obj.transform.position = position;
        obj.transform.localScale = new Vector3(0.23f, 0.12f, 1f);
        obj.transform.rotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(shotDirection.y, shotDirection.x) * Mathf.Rad2Deg);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        if (squareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            squareSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
        }
        sr.sprite = squareSprite;
        sr.color = new Color(0.27f, 0.98f, 1f);
        sr.sortingOrder = 13;

        GJFieldTurretBullet bullet = obj.AddComponent<GJFieldTurretBullet>();
        bullet.direction = shotDirection.normalized;
        bullet.room = targetRoom;
        bullet.speed = Mathf.Max(0.1f, shotSpeed);
        bullet.damage = Mathf.Max(0.1f, shotDamage);
        bullet.endTime = Time.time + Mathf.Max(0.1f, lifetime);
    }

    void Update()
    {
        if (room == null || Time.time >= endTime)
        {
            Destroy(gameObject);
            return;
        }
        Vector2 start = transform.position;
        float distance = speed * Time.deltaTime;
        RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, distance);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D c = hit.collider;
            if (c == null) continue;

            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target is PlayerStats) continue;
            Component component = target as Component;
            if (component != null && component.GetComponentInParent<Room>() == room)
            {
                target.TakeDamage(damage, direction);
                Destroy(gameObject);
                return;
            }
            if (c.isTrigger) continue;
            Destroy(gameObject); // Wall or prop
            return;
        }
        transform.position = start + direction * distance;
    }
}
