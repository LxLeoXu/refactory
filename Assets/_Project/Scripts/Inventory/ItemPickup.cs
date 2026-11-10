using UnityEngine;

// Daj na prefab loot predmetu: Collider2D -> Is Trigger = true.
public class ItemPickup : MonoBehaviour
{
    public string itemId = "iron";
    [Min(1)] public int amount = 1;

    private bool collected;

    public void Setup(string id, int count)
    {
        itemId = id;
        amount = Mathf.Max(1, count);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;

        Inventory inventory = other.GetComponentInParent<Inventory>();
        if (inventory == null) return;

        if (ItemDatabase.Instance != null && ItemDatabase.Instance.GetItem(itemId) == null)
        {
            Debug.LogWarning("ItemPickup: ID nie je v items.json: " + itemId);
            return;
        }

        collected = true;
        inventory.AddItem(itemId, amount);
        Debug.Log("Ziskane: " + itemId + " x" + amount);
        Destroy(gameObject);
    }
}
