using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RecipeIngredient
{
    public string itemId;
    public int amount;
}

[Serializable]
public class CraftingRecipe
{
    public string id;
    public string name;
    public RecipeIngredient[] ingredients;
    public string resultItemId;
    public int resultAmount = 1;
}

[Serializable]
public class CraftingRecipeCollection
{
    public CraftingRecipe[] recipes;
}

// Add this component to GameManager. Reads Assets/Resources/recipes.json.
public class CraftingSystem : MonoBehaviour
{
    public Inventory inventory;
    public List<CraftingRecipe> recipes = new List<CraftingRecipe>();

    void Awake()
    {
        TextAsset file = Resources.Load<TextAsset>("recipes");
        if (file == null)
        {
            Debug.LogError("CraftingSystem: Missing Assets/Resources/recipes.json");
            return;
        }
        try
        {
            CraftingRecipeCollection data = JsonUtility.FromJson<CraftingRecipeCollection>(file.text);
            if (data == null || data.recipes == null)
            {
                Debug.LogError("CraftingSystem: recipes.json has no recipes array.");
                return;
            }
            recipes.AddRange(data.recipes);
            Debug.Log("CraftingSystem: Loaded " + recipes.Count + " recipes.");
        }
        catch (Exception ex)
        {
            Debug.LogError("CraftingSystem: Invalid recipes.json: " + ex.Message);
        }
    }

    void Start()
    {
        if (inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) inventory = player.GetComponent<Inventory>();
        }
    }

    public bool CanCraft(CraftingRecipe recipe)
    {
        if (inventory == null || recipe == null || recipe.ingredients == null ||
            recipe.ingredients.Length == 0 || string.IsNullOrEmpty(recipe.resultItemId) ||
            recipe.resultAmount <= 0) return false;

        // Aggregate repeated ingredients to avoid under-counting duplicate IDs.
        Dictionary<string, int> required = new Dictionary<string, int>();
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null || string.IsNullOrEmpty(ingredient.itemId) || ingredient.amount <= 0)
                return false;
            if (!required.ContainsKey(ingredient.itemId)) required[ingredient.itemId] = 0;
            required[ingredient.itemId] += ingredient.amount;
        }
        foreach (var pair in required)
            if (!inventory.HasItem(pair.Key, pair.Value)) return false;
        return true;
    }

    public bool Craft(CraftingRecipe recipe)
    {
        if (!CanCraft(recipe)) return false;
        Dictionary<string, int> required = new Dictionary<string, int>();
        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (!required.ContainsKey(ingredient.itemId)) required[ingredient.itemId] = 0;
            required[ingredient.itemId] += ingredient.amount;
        }
        foreach (var pair in required) inventory.RemoveItem(pair.Key, pair.Value);
        inventory.AddItem(recipe.resultItemId, recipe.resultAmount);
        Debug.Log("Crafted " + recipe.resultItemId + " x" + recipe.resultAmount);
        return true;
    }
}
