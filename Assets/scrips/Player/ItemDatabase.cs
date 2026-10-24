using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string type;
    public int maxStack = 999;
}

[Serializable]
public class ItemCollection
{
    public ItemData[] items;
}

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    private readonly Dictionary<string, ItemData> itemsById = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadItems();
    }

    private void LoadItems()
    {
        TextAsset file = Resources.Load<TextAsset>("items");
        if (file == null)
        {
            Debug.LogError("ItemDatabase: Nenasiel sa Assets/Resources/items.json");
            return;
        }

        ItemCollection data = JsonUtility.FromJson<ItemCollection>(file.text);
        if (data == null || data.items == null)
        {
            Debug.LogError("ItemDatabase: Neplatny obsah items.json");
            return;
        }

        itemsById.Clear();
        foreach (ItemData item in data.items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.id)) continue;
            if (itemsById.ContainsKey(item.id))
            {
                Debug.LogWarning("ItemDatabase: Duplicitne ID: " + item.id);
                continue;
            }
            itemsById.Add(item.id, item);
        }
        Debug.Log("ItemDatabase: Nacitanych predmetov: " + itemsById.Count);
    }

    public ItemData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        itemsById.TryGetValue(id, out ItemData item);
        return item;
    }
}
