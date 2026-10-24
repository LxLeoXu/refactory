using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FactoryMachine : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Inventory inventory;
    public CraftingSystem crafting;

    [Header("Machine Settings")]
    public string machineName = "Furnace";
    public string recipeId = "metal_plate_recipe";

    public float productionTime = 5f;
    public float interactionRange = 2.5f;

    private bool isProducing;
    private bool isReady;

    private float timer;

    private string outputItemId;
    private int outputAmount;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");

            if (p != null)
                player = p.transform;
        }

        if (inventory == null && player != null)
            inventory = player.GetComponent<Inventory>();

        if (crafting == null)
            crafting = FindFirstObjectByType<CraftingSystem>();

        if (crafting == null)
            Debug.LogError("FactoryMachine: CraftingSystem missing!");
    }

    void Update()
    {
        // Machine production
        if (isProducing)
        {
            timer -= Time.deltaTime;

            if (timer <= 0f)
            {
                timer = 0f;
                isProducing = false;
                isReady = true;

                Debug.Log(machineName + ": Production finished!");
            }
        }

        // Interaction
        if (player == null || Keyboard.current == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance > interactionRange)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (isReady)
                CollectItem();
            else if (!isProducing)
                StartProduction();
        }
    }

    CraftingRecipe GetRecipe()
    {
        if (crafting == null)
            return null;

        return crafting.recipes.Find(
            r => r != null && r.id == recipeId
        );
    }

    Dictionary<string, int> GetIngredients(CraftingRecipe recipe)
    {
        if (recipe == null ||
            recipe.ingredients == null ||
            recipe.ingredients.Length == 0 ||
            string.IsNullOrEmpty(recipe.resultItemId) ||
            recipe.resultAmount <= 0)
            return null;

        Dictionary<string, int> costs =
            new Dictionary<string, int>();

        foreach (RecipeIngredient ingredient in recipe.ingredients)
        {
            if (ingredient == null ||
                string.IsNullOrEmpty(ingredient.itemId) ||
                ingredient.amount <= 0)
                return null;

            if (!costs.ContainsKey(ingredient.itemId))
                costs[ingredient.itemId] = 0;

            costs[ingredient.itemId] += ingredient.amount;
        }

        return costs;
    }

    bool CanProduce()
    {
        if (inventory == null || isProducing || isReady)
            return false;

        CraftingRecipe recipe = GetRecipe();
        var costs = GetIngredients(recipe);

        if (costs == null)
            return false;

        foreach (var cost in costs)
        {
            if (!inventory.HasItem(cost.Key, cost.Value))
                return false;
        }

        return true;
    }

    public void StartProduction()
    {
        if (!CanProduce())
        {
            Debug.Log("Not enough materials!");
            return;
        }

        CraftingRecipe recipe = GetRecipe();
        var costs = GetIngredients(recipe);

        // Consume ingredients
        foreach (var cost in costs)
        {
            inventory.RemoveItem(cost.Key, cost.Value);
        }

        outputItemId = recipe.resultItemId;
        outputAmount = recipe.resultAmount;

        timer = Mathf.Max(0.1f, productionTime);
        isProducing = true;

        Debug.Log(machineName + ": Production started!");
    }

    public void CollectItem()
    {
        if (!isReady || inventory == null)
            return;

        inventory.AddItem(outputItemId, outputAmount);

        Debug.Log(
            "Collected: " + outputItemId + " x" + outputAmount
        );

        isReady = false;
        outputItemId = null;
        outputAmount = 0;
    }

    void OnGUI()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.position
        );

        if (distance > interactionRange)
            return;

        float width = Mathf.Min(320f, Screen.width - 20f);
        float x = Screen.width - width - 10f;
        float y = Mathf.Max(10f, Screen.height - 210f);

        string text = machineName + "\n\n";

        if (isReady)
        {
            text += "Production complete!\n";
            text += outputItemId + " x" + outputAmount;
            text += "\n\n[E] Collect";
        }
        else if (isProducing)
        {
            text += "Producing...\n";
            text += "Time remaining: " + timer.ToString("0.0") + "s";
        }
        else
        {
            CraftingRecipe recipe = GetRecipe();

            if (recipe == null)
            {
                text += "Recipe not found!";
            }
            else
            {
                text += "Result: " + recipe.resultItemId + "\n";

                var costs = GetIngredients(recipe);

                if (costs != null && inventory != null)
                {
                    foreach (var cost in costs)
                    {
                        text += cost.Key + ": ";
                        text += inventory.GetItemCount(cost.Key);
                        text += "/" + cost.Value + "\n";
                    }
                }

                text += CanProduce()
                    ? "\n[E] Start Production"
                    : "\nNot enough materials";
            }
        }

        GUI.Box(new Rect(x, y, width, 190f), text);
    }
}