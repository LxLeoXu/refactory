using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int amount;
    }

    public List<InventoryItem> items = new List<InventoryItem>();
    public event Action OnInventoryChanged;

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        InventoryItem item = items.Find(i => i.itemId == itemId);
        if (item != null) item.amount += amount;
        else items.Add(new InventoryItem { itemId = itemId, amount = amount });
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(string itemId, int amount)
    {
        if (amount <= 0) return false;
        InventoryItem item = items.Find(i => i.itemId == itemId);
        if (item == null || item.amount < amount) return false;
        item.amount -= amount;
        if (item.amount == 0) items.Remove(item);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public int GetItemCount(string itemId)
    {
        InventoryItem item = items.Find(i => i.itemId == itemId);
        return item == null ? 0 : item.amount;
    }

    public bool HasItem(string itemId, int amount) => amount >= 0 && GetItemCount(itemId) >= amount;
}
