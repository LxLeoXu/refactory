// Item prefab generator for Unity 6.3 LTS.
// Requires your existing ItemPickup.cs (from the loot system) in the project.
// Run: Tools > GameJam > Generate Item Prefabs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class GameJamItemPrefabSetup
{
    private static readonly string[] ItemIds =
    {
        "iron", "scrap", "crystal", "wood", "metal_plate",
        "weapon_part", "armor_plate", "health_potion",
        "weapon_upgrade", "armor_upgrade"
    };

    private const string IconFolder = "Assets/Resources/ItemIcons";
    private const string PrefabFolder = "Assets/GameJamItems/Prefabs";

    [MenuItem("Tools/GameJam/Generate Item Prefabs")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(IconFolder))
        {
            Debug.LogError("Missing " + IconFolder + ". Copy the PNG files from the pack.");
            return;
        }

        Directory.CreateDirectory(PrefabFolder);
        AssetDatabase.Refresh();
        int created = 0;

        foreach (string id in ItemIds)
        {
            string iconPath = IconFolder + "/" + id + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Missing icon: " + iconPath);
                continue;
            }

            // Make PNG a crisp, transparent Sprite (not a default Texture).
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null)
            {
                Debug.LogWarning("Could not import sprite: " + iconPath);
                continue;
            }

            GameObject go = new GameObject("Pickup_" + id);
            try
            {
                go.transform.localScale = Vector3.one * 0.8f;

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 6;

                var collider = go.AddComponent<CircleCollider2D>();
                collider.isTrigger = true;
                collider.radius = 0.48f;

                var body = go.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.simulated = true;

                // Uses EXISTING ItemPickup from GameJam_LootSystem.zip:
                var pickup = go.AddComponent<ItemPickup>();
                pickup.itemId = id;
                pickup.amount = 1;

                // If EnemyLoot overrides itemId, sprite changes to match.
                go.AddComponent<GameJamPickupVisual>();

                string prefabPath = PrefabFolder + "/Pickup_" + id + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                created++;
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GameJam Items: generated " + created + " pickup prefabs at " + PrefabFolder);
        EditorUtility.DisplayDialog("GameJam Items", "Created " + created + " pickup prefabs!\n" + PrefabFolder, "OK");
    }

    [MenuItem("Tools/GameJam/Place Test Pickups Near Player")]
    public static void SpawnTestItems()
    {
        GameObject player = null;
        // In an existing project the Player tag should already be configured.
        try { player = GameObject.FindGameObjectWithTag("Player"); }
        catch (UnityException) { /* Player tag may not exist yet */ }

        Vector3 center = player != null ? player.transform.position : Vector3.zero;
        GameObject root = new GameObject("GameJam_Test_Pickups");
        Undo.RegisterCreatedObjectUndo(root, "Create test pickup group");
        int count = 0;
        foreach (string id in ItemIds)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/Pickup_" + id + ".prefab");
            if (prefab == null) continue;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) continue;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn test pickup");
            instance.transform.SetParent(root.transform);
            float angle = count * Mathf.PI * 2f / ItemIds.Length;
            instance.transform.position = center + new Vector3(Mathf.Cos(angle) * 3.5f, Mathf.Sin(angle) * 3.0f, 0f);
            count++;
        }
        Debug.Log("GameJam Items: placed " + count + " test pickups in scene.");
    }
}
#endif
