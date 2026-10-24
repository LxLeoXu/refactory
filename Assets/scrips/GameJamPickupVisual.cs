using UnityEngine;

// Keeps the pickup sprite consistent with ItemPickup.itemId.
// Especially useful because EnemyLoot.Setup(...) can override the ID
// on any generic pickup prefab immediately after Instantiate().
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(ItemPickup))]
public class GameJamPickupVisual : MonoBehaviour
{
    private void Start()
    {
        ItemPickup pickup = GetComponent<ItemPickup>();
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (pickup == null || spriteRenderer == null) return;

        Sprite icon = Resources.Load<Sprite>("ItemIcons/" + pickup.itemId);
        if (icon != null) spriteRenderer.sprite = icon;
        else Debug.LogWarning("Missing item icon for ID: " + pickup.itemId, this);
    }
}
