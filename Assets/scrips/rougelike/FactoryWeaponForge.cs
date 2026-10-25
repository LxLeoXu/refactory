using UnityEngine;
using UnityEngine.InputSystem;

// Put this on the EXISTING Assembler scene object. [E] original production, [F] weapon crafting.
// Uses the existing CraftingSystem and recipes.json, not a second inventory.
public class FactoryWeaponForge : MonoBehaviour
{
    [Min(1f)] public float interactionRange = 2.5f;
    public Transform player;
    public CraftingSystem crafting;

    bool showMenu;
    readonly string[] weaponRecipes = {
        "factory_scrap_shotgun_recipe",
        "factory_crystal_rifle_recipe",
        "factory_furnace_cannon_recipe"
    };

    void Start()
    {
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (crafting == null) crafting = FindFirstObjectByType<CraftingSystem>();
    }

    void Update()
    {
        if (player == null || crafting == null) return;
        bool near = Vector2.Distance(transform.position, player.position) <= interactionRange;
        if (!near) { showMenu = false; return; }
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            showMenu = !showMenu;
    }

    void OnGUI()
    {
        if (player == null || crafting == null || crafting.inventory == null) return;
        if (Vector2.Distance(transform.position, player.position) > interactionRange) return;

        if (!showMenu)
        {
            GUI.Box(new Rect(Mathf.Max(10f, Screen.width - 310f), 200f, 285f, 35f),
                "[F] Weapon Forge - craft weapons");
            return;
        }

        float width = Mathf.Min(390f, Screen.width - 20f);
        float h = Mathf.Min(420f, Screen.height - 30f);
        Rect area = new Rect(Mathf.Max(10f, Screen.width - width - 10f), 30f, width, h);
        GUI.Box(area, "WEAPON FORGE  [F] close");
        GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 30f, area.width - 24f, area.height - 42f));
        foreach (string id in weaponRecipes)
        {
            CraftingRecipe recipe = crafting.recipes.Find(r => r != null && r.id == id);
            if (recipe == null)
            {
                GUILayout.Label("Missing recipe: " + id);
                continue;
            }
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(recipe.name);
            foreach (RecipeIngredient ingredient in recipe.ingredients)
                if (ingredient != null)
                    GUILayout.Label(ingredient.itemId + ": " + crafting.inventory.GetItemCount(ingredient.itemId) + "/" + ingredient.amount);
            bool canCraft = crafting.CanCraft(recipe);
            GUI.enabled = canCraft;
            if (GUILayout.Button(canCraft ? "CRAFT" : "Missing materials", GUILayout.Height(26f)))
                crafting.Craft(recipe);
            GUI.enabled = true;
            GUILayout.EndVertical();
        }
        GUILayout.EndArea();
    }
}
