#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only setup. Does not replace or duplicate existing runtime scripts.
public static class REFACTORYChestAudioSetup011
{
    private const string Closed = "Assets/_Project/Art/Textures/chest_closed_86b9f351.png";
    private const string Open = "Assets/_Project/Art/Textures/chest_open_f134034b.png";
    private const string PrefabFolder = "Assets/REFACTORY_Patch011";
    private const string Prefabs = "Assets/REFACTORY_Patch011/Prefabs";
    private const string PrefabPath = "Assets/REFACTORY_Patch011/Prefabs/LootChest_Patch011.prefab";
    private const string Tracks = "Assets/Resources/GameJamMusic/Tracks/";

    [MenuItem("Tools/REFACTORY/Patch 0.1.1/Setup Chest + Music (Current Scene)")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("REFACTORY 0.1.1: Spusti tento nastroj mimo Play Mode.");
            return;
        }

        bool chestOk = SetupChest();
        bool musicOk = SetupMusic();
        int listeners = 0;
        foreach (AudioListener listener in UnityEngine.Object.FindObjectsByType<AudioListener>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (listener != null && listener.isActiveAndEnabled) listeners++;
        }
        if (listeners != 1)
            Debug.LogWarning("REFACTORY audio: aktivnych AudioListenerov = " + listeners +
                             ". V kazdej scene ma byt prave jeden (zvycajne Main Camera).");

        if (chestOk || musicOk)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        Debug.Log("REFACTORY 0.1.1: Chest setup = " + (chestOk ? "OK" : "CHECK LOG") +
                  "; Music setup = " + (musicOk ? "OK" : "CHECK LOG") +
                  ". Uloz scenu (Ctrl+S) a vyskusaj Play Mode.");
    }

    private static bool SetupChest()
    {
        if (!System.IO.File.Exists(Closed) || !System.IO.File.Exists(Open))
        {
            Debug.LogWarning("REFACTORY chest: chyba chest_closed.png alebo chest_open.png v Assets/_Project/Art/Textures.");
            return false;
        }

        SetSpriteImport(Closed);
        SetSpriteImport(Open);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Closed);
        if (sprite == null)
        {
            Debug.LogError("REFACTORY chest: nepodarilo sa nacitat sprite. Over Sprite (2D and UI) / Single.");
            return false;
        }

        Type lootChestType = FindRuntimeComponent("LootChest");
        if (lootChestType == null)
        {
            Debug.LogWarning("REFACTORY chest: v projekte chyba skript LootChest.cs. " +
                             "Grafiku mas importovanu; na existujucej truhlici mozes Sprite priradit rucne.");
            return false;
        }

        EnsureFolder("Assets", "REFACTORY_Patch011");
        EnsureFolder(PrefabFolder, "Prefabs");

        GameObject temp = new GameObject("LootChest_Patch011");
        try
        {
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.65f);
            temp.AddComponent(lootChestType);

            // A new prefab path is used; existing Chest.prefab and its GUID stay untouched.
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("REFACTORY chest: nepodarilo sa ulozit prefab.");
                return false;
            }

            int connected = 0;
            foreach (MonoBehaviour component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (component == null || component.GetType().Name != "DungeonGenerator") continue;
                SerializedObject so = new SerializedObject(component);
                SerializedProperty chestProperty = so.FindProperty("chestPrefab");
                if (chestProperty == null)
                {
                    Debug.LogWarning("DungeonGenerator nema serializovane pole chestPrefab: " + component.name);
                    continue;
                }
                Undo.RecordObject(component, "Assign REFACTORY Loot Chest");
                chestProperty.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(component);
                connected++;
            }
            if (connected == 0)
                Debug.LogWarning("REFACTORY chest: prefab hotovy, ale v aktualnej scene nie je DungeonGenerator. " +
                                 "Otvor LevelScene a spusti setup znova.");
            else
                Debug.Log("REFACTORY chest: prefab vytvoreny, priradeny do " + connected + " DungeonGenerator objektov.");
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temp);
        }
    }

    private static bool SetupMusic()
    {
        Type musicType = FindRuntimeComponent("GJGameMusic");
        if (musicType == null)
        {
            Debug.LogWarning("REFACTORY music: chyba existujuci GJGameMusic.cs. " +
                             "Obnov ho zo starsieho balika hudby, ale nevytvaraj duplicitu triedy.");
            return false;
        }

        AudioClip baseClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Tracks + "Base_Factory.ogg");
        AudioClip dungeonClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Tracks + "Dungeon_Exploration.ogg");
        AudioClip bossClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Tracks + "ForgeHeart_Boss.ogg");
        if (baseClip == null || dungeonClip == null || bossClip == null)
        {
            Debug.LogWarning("REFACTORY music: chyba niektora OGG stopa. Musi byt v " + Tracks);
            return false;
        }

        GameObject manager = null;
        foreach (Transform t in UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t != null && t.name == "GameJamMusicManager" && t.gameObject.scene == SceneManager.GetActiveScene())
            {
                manager = t.gameObject;
                break;
            }
        }
        if (manager == null)
        {
            manager = new GameObject("GameJamMusicManager");
            Undo.RegisterCreatedObjectUndo(manager, "Create REFACTORY music manager");
        }
        if (!manager.activeSelf) manager.SetActive(true);

        Component music = manager.GetComponent(musicType);
        if (music == null) music = Undo.AddComponent(manager, musicType);
        SerializedObject so = new SerializedObject(music);
        SetObject(so, "baseMusic", baseClip);
        SetObject(so, "dungeonMusic", dungeonClip);
        SetObject(so, "bossMusic", bossClip);
        SetBool(so, "autoDetectRoom", true);
        SetBool(so, "persistBetweenScenes", true);
        SerializedProperty vol = so.FindProperty("volume");
        if (vol != null && vol.floatValue <= 0.0001f) vol.floatValue = 0.32f;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(music);
        Debug.Log("REFACTORY music: 3 AudioClipy priradene v scene " + SceneManager.GetActiveScene().name + ".");
        return true;
    }

    private static Type FindRuntimeComponent(string simpleName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(simpleName);
            if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type)) return type;
        }
        return null;
    }

    private static void SetObject(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null) property.objectReferenceValue = value;
        else Debug.LogWarning("Music component nema property: " + propertyName);
    }
    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
    }
    private static void SetSpriteImport(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null) return;
        bool changed = importer.textureType != TextureImporterType.Sprite ||
                       importer.spriteImportMode != SpriteImportMode.Single ||
                       importer.filterMode != FilterMode.Point ||
                       importer.textureCompression != TextureImporterCompression.Uncompressed ||
                       importer.spritePixelsPerUnit != 32;
        if (!changed) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 32f;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }
    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
