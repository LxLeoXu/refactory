#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GJForgeHeartSetup
{
    const string Root = "Assets/GameJamBoss";
    const string Prefabs = "Assets/GameJamBoss/Prefabs";
    const string SpriteFile = "Assets/GameJamBoss/Sprites/forge_heart.png";

    [MenuItem("Tools/GameJam/Add Forge Heart Boss")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "GameJamBoss");
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder(Root, "Prefabs");
        Sprite art = LoadArt();
        if (art == null)
        {
            Debug.LogError("Forge Heart: Nenajdeny sprite: " + SpriteFile);
            return;
        }

        // Nevytvarame duplikat pri opakovanom kliknuti v menu.
        string forgePath = Prefabs + "/Boss_ForgeHeart.prefab";
        GameObject forge = AssetDatabase.LoadAssetAtPath<GameObject>(forgePath);
        if (forge == null)
        {
            GameObject go = new GameObject("Boss_ForgeHeart");
            try
            {
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = art;
                sr.color = Color.white;
                sr.sortingOrder = 4;
                go.transform.localScale = Vector3.one * 1.25f;
                Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                CircleCollider2D col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = false;
                col.radius = 0.78f;
                go.AddComponent<GJForgeHeartBoss>();
                forge = PrefabUtility.SaveAsPrefabAsset(go, forgePath);
            }
            finally { Object.DestroyImmediate(go); }
        }

        string selectorPath = Prefabs + "/Boss_RandomSelector.prefab";
        GameObject selector = AssetDatabase.LoadAssetAtPath<GameObject>(selectorPath);
        GameObject oldBoss = null;
        int changed = 0;
        DungeonGenerator[] dungeons = Object.FindObjectsByType<DungeonGenerator>(FindObjectsSortMode.None);
        foreach (DungeonGenerator dungeon in dungeons)
        {
            GameObject configured = dungeon.bossPrefab;
            if (configured != null && configured != selector && configured != forge)
            {
                GJRandomBossSelector previous = configured.GetComponent<GJRandomBossSelector>();
                oldBoss = previous != null ? previous.originalBossPrefab : configured;
                if (oldBoss != null) break;
            }
        }
        if (oldBoss == null && selector != null)
        {
            GJRandomBossSelector old = selector.GetComponent<GJRandomBossSelector>();
            if (old != null) oldBoss = old.originalBossPrefab;
        }

        GameObject temp = selector != null ? PrefabUtility.InstantiatePrefab(selector) as GameObject
            : new GameObject("Boss_RandomSelector");
        if (temp == null) temp = new GameObject("Boss_RandomSelector");
        try
        {
            GJRandomBossSelector pool = temp.GetComponent<GJRandomBossSelector>();
            if (pool == null) pool = temp.AddComponent<GJRandomBossSelector>();
            pool.originalBossPrefab = oldBoss;
            pool.forgeHeartBossPrefab = forge;
            PrefabUtility.SaveAsPrefabAsset(temp, selectorPath);
        }
        finally { Object.DestroyImmediate(temp); }

        selector = AssetDatabase.LoadAssetAtPath<GameObject>(selectorPath);
        foreach (DungeonGenerator dungeon in dungeons)
        {
            if (dungeon.bossPrefab == selector) continue;
            Undo.RecordObject(dungeon, "Assign Forge Heart boss pool");
            dungeon.bossPrefab = selector;
            EditorUtility.SetDirty(dungeon);
            if (dungeon.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(dungeon.gameObject.scene);
            changed++;
        }
        AssetDatabase.SaveAssets();
        Selection.activeObject = selector;
        EditorGUIUtility.PingObject(selector);
        if (dungeons.Length == 0)
            Debug.LogWarning("Forge Heart: Prefaby vytvorene. V scene nie je DungeonGenerator; prirad Boss_RandomSelector do Boss Prefab rucne.");
        else
            Debug.Log("Forge Heart: hotovo. DungeonGenerator: " + dungeons.Length + ", upravene: " + changed + ". Stary boss ostava, novy sa vybera nahodne.");
    }

    static Sprite LoadArt()
    {
        AssetDatabase.ImportAsset(SpriteFile, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = AssetImporter.GetAtPath(SpriteFile) as TextureImporter;
        if (importer == null) return null;
        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 24f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        settings.spriteMeshType = SpriteMeshType.FullRect; // Unity 6.3
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFile);
    }
}
#endif
