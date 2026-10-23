using UnityEngine;
using UnityEngine.InputSystem;

// Temporary no-Canvas crafting window. Toggle with C. No old Input API.
public class CraftingUI : MonoBehaviour
{
    public CraftingSystem crafting;
    public bool showCrafting = false;
    private Vector2 scroll;

    void Start()
    {
        if (crafting == null) crafting = GetComponent<CraftingSystem>();
        if (crafting == null) crafting = FindFirstObjectByType<CraftingSystem>();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            showCrafting = !showCrafting;
    }

    void OnGUI()
    {
        if (!showCrafting) return;

        float width = Mathf.Min(440f, Screen.width - 20f);
        Rect area = new Rect(10, Mathf.Min(145f, Screen.height * 0.25f), width,
            Mathf.Min(410f, Screen.height * 0.65f));
        GUI.Box(area, "CRAFTING  (C - close)");

        GUILayout.BeginArea(new Rect(area.x + 12, area.y + 27, area.width - 24, area.height - 39));
        if (crafting == null || crafting.inventory == null)
        {
            GUILayout.Label("Missing CraftingSystem or Player Inventory.");
        }
        else if (crafting.recipes.Count == 0)
        {
            GUILayout.Label("No crafting recipes loaded. Check recipes.json.");
        }
        else
        {
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (CraftingRecipe recipe in crafting.recipes)
            {
                if (recipe == null) continue;
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(string.IsNullOrEmpty(recipe.name) ? recipe.id : recipe.name);
                if (recipe.ingredients != null)
                {
                    foreach (RecipeIngredient ingredient in recipe.ingredients)
                    {
                        if (ingredient == null) continue;
                        int owned = crafting.inventory.GetItemCount(ingredient.itemId);
                        GUILayout.Label("  " + ingredient.itemId + ": " + owned + "/" + ingredient.amount);
                    }
                }
                GUILayout.Label("Result: " + recipe.resultItemId + " x" + recipe.resultAmount);
                bool canCraft = crafting.CanCraft(recipe);
                GUI.enabled = canCraft;
                if (GUILayout.Button(canCraft ? "CRAFT" : "Missing materials", GUILayout.Height(30)))
                    crafting.Craft(recipe);
                GUI.enabled = true;
                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndArea();
    }
}
