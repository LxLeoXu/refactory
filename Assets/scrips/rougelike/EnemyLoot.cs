using UnityEngine;

// Pridaj na prefab nepriatela a prirad prefab s ItemPickup komponentom.
public class EnemyLoot : MonoBehaviour
{
    public GameObject pickupPrefab;
    public string itemId = "iron";
    [Min(1)] public int minAmount = 1;
    [Min(1)] public int maxAmount = 3;
    [Range(0f, 1f)] public float dropChance = 0.75f;

    public void Drop()
    {
        if (pickupPrefab == null || Random.value > dropChance) return;

        GameObject go = Instantiate(pickupPrefab, transform.position, Quaternion.identity);
        ItemPickup pickup = go.GetComponent<ItemPickup>();
        if (pickup == null)
        {
            Debug.LogError("EnemyLoot: pickupPrefab nema komponent ItemPickup!");
            Destroy(go);
            return;
        }
        pickup.Setup(itemId, Random.Range(minAmount, Mathf.Max(minAmount, maxAmount) + 1));
    }
}
