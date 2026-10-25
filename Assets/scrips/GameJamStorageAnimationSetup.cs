#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// OPRAVA: funguje aj ked su obrazky v inom priecinku alebo uplne chybaju.
// Pouziva EXISTUJUCE FactoryMachine, FactoryMachineLight, FactoryMachineAnimator
// a BaseStorageChest. Nevytvara ani nekopiruje dalsie C# skripty.
public static class GameJamStorageAnimationSetup
{
    const string SpritesFolder = "Assets/GameJamExtras/Sprites";
    const string PrefabsFolder = "Assets/GameJamExtras/Prefabs";
    const string StoragePrefabPath = PrefabsFolder + "/BaseStorageChest.prefab";

    [MenuItem("Tools/GameJam/Setup Storage and Machine Animations")]
    public static void Setup()
    {
        Directory.CreateDirectory(SpritesFolder);
        Directory.CreateDirectory(PrefabsFolder);
        AssetDatabase.Refresh();

        // Najprv hlada skutocne PNG v celom Assets. Ak chybaju,
        // vytvori vlastne jednoduche pixel-art nahrady (bez chyby v Console).
        Sprite chest = FindOrGenerate("Storage_Chest.png");
        Sprite gear = FindOrGenerate("Gear.png");
        Sprite steam = FindOrGenerate("Steam.png");
        Sprite glow = FindOrGenerate("WorkGlow.png");

        if (chest == null)
        {
            Debug.LogError("GameJam Storage: nepodarilo sa nacitat ani vytvorit sprite truhlice.");
            return;
        }

        CreateOrUpdateStoragePrefab(chest);
        PlaceStorageInBase();

        // Vylepsenie prefabov strojov, bez vytvarania duplicitnych strojov.
        int prefabCount = 0;
        string[] folders = { "Assets/GeneratedFactory/Prefabs", "Assets/GameJamFactory/Prefabs" };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder)) continue;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (root.GetComponent<FactoryMachine>() == null) continue;
                    AddAnimation(root, gear, steam, glow);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    prefabCount++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        // Vylepsenie strojov uz umiestnenych v otvorenych scenach.
        int sceneCount = 0;
        FactoryMachine[] machines = UnityEngine.Object.FindObjectsByType<FactoryMachine>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (FactoryMachine machine in machines)
        {
            if (!machine.gameObject.scene.IsValid()) continue;
            AddAnimation(machine.gameObject, gear, steam, glow);
            EditorSceneManager.MarkSceneDirty(machine.gameObject.scene);
            sceneCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("GameJam: Storage a animacie su pripravene. Stroje v scene: " +
                  sceneCount + ", prefaby: " + prefabCount + ".");
        EditorUtility.DisplayDialog("GameJam – Hotovo",
            "Sklad a animacie boli nastavene.\nStroje v scene: " + sceneCount +
            "\nPrefaby: " + prefabCount +
            "\n\nAk obrazky chybali, vytvorili sa zastupne PNG v " + SpritesFolder +
            ".\n\nSklad otvoris klavesom F.", "OK");
    }

    static Sprite FindOrGenerate(string fileName)
    {
        string desiredPath = SpritesFolder + "/" + fileName;
        Sprite found = ImportSprite(desiredPath);
        if (found != null) return found;

        // Nevyzaduje pevnu cestu: napriklad Assets/scripts/factory/GameJamExtras/Sprites/...
        string name = Path.GetFileNameWithoutExtension(fileName);
        foreach (string guid in AssetDatabase.FindAssets(name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                continue;
            found = ImportSprite(path);
            if (found != null)
            {
                Debug.Log("GameJam: obrazok " + fileName + " najdeny v " + path);
                return found;
            }
        }

        Debug.LogWarning("GameJam: " + fileName + " chyba, vytvaram nahradny sprite.");
        CreatePlaceholderPng(desiredPath, name);
        return ImportSprite(desiredPath);
    }

    static Sprite ImportSprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return null;

        if (imp.textureType != TextureImporterType.Sprite ||
            imp.spriteImportMode != SpriteImportMode.Single ||
            !Mathf.Approximately(imp.spritePixelsPerUnit, 32f) ||
            imp.filterMode != FilterMode.Point || imp.mipmapEnabled)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Vytvorenie realnych PNG assetov, aby sprite prezil ulozenie prefabu aj build.
    static void CreatePlaceholderPng(string path, string name)
    {
        const int size = 32;
        Color32[] pixels = new Color32[size * size];
        Color32 dark = new Color32(40, 45, 56, 255);
        Color32 metal = new Color32(110, 123, 137, 255);
        Color32 shine = new Color32(193, 209, 216, 255);
        Color32 brown = new Color32(111, 67, 39, 255);
        Color32 wood = new Color32(165, 104, 54, 255);
        Color32 gold = new Color32(242, 176, 52, 255);

        if (name == "Storage_Chest")
        {
            FillRect(pixels, 4, 8, 27, 24, dark);
            FillRect(pixels, 6, 10, 25, 22, brown);
            FillRect(pixels, 7, 11, 24, 15, wood);
            FillRect(pixels, 7, 17, 24, 21, wood);
            FillRect(pixels, 7, 10, 9, 22, metal);
            FillRect(pixels, 22, 10, 24, 22, metal);
            FillRect(pixels, 14, 15, 18, 19, gold);
            FillRect(pixels, 15, 16, 17, 18, dark);
        }
        else if (name == "Gear")
        {
            FillRect(pixels, 12, 2, 19, 29, metal);
            FillRect(pixels, 2, 12, 29, 19, metal);
            FillRect(pixels, 7, 7, 24, 24, dark);
            FillRect(pixels, 9, 9, 22, 22, shine);
            FillRect(pixels, 13, 13, 18, 18, dark);
            FillRect(pixels, 14, 14, 17, 17, new Color32(0, 0, 0, 0));
        }
        else if (name == "Steam")
        {
            FillCircle(pixels, 12, 8, 5, new Color32(205, 219, 229, 160));
            FillCircle(pixels, 20, 15, 7, new Color32(205, 219, 229, 140));
            FillCircle(pixels, 13, 23, 6, new Color32(231, 238, 242, 110));
        }
        else // WorkGlow
        {
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f));
                    byte alpha = (byte)Mathf.RoundToInt(160f * Mathf.Clamp01(1f - distance / 15f));
                    pixels[y * size + x] = new Color32(255, 190, 80, alpha);
                }
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }

    static void FillRect(Color32[] pixels, int x1, int y1, int x2, int y2, Color32 color)
    {
        for (int y = Mathf.Max(0, y1); y <= Mathf.Min(31, y2); y++)
            for (int x = Mathf.Max(0, x1); x <= Mathf.Min(31, x2); x++)
                pixels[y * 32 + x] = color;
    }

    static void FillCircle(Color32[] pixels, int cx, int cy, int radius, Color32 color)
    {
        for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(31, cy + radius); y++)
            for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(31, cx + radius); x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                    pixels[y * 32 + x] = color;
    }

    static void CreateOrUpdateStoragePrefab(Sprite sprite)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(StoragePrefabPath);
        GameObject root = existing != null
            ? PrefabUtility.LoadPrefabContents(StoragePrefabPath)
            : new GameObject("BaseStorageChest");
        try
        {
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr == null) sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 4;

            BoxCollider2D col = root.GetComponent<BoxCollider2D>();
            if (col == null) col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.7f, 1.25f);
            col.offset = new Vector2(0f, -0.18f);

            if (root.GetComponent<BaseStorageChest>() == null)
                root.AddComponent<BaseStorageChest>();

            PrefabUtility.SaveAsPrefabAsset(root, StoragePrefabPath);
        }
        finally
        {
            if (existing == null) UnityEngine.Object.DestroyImmediate(root);
            else PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PlaceStorageInBase()
    {
        GameObject room = GameObject.Find("BaseRoom");
        if (room == null)
        {
            Debug.LogWarning("GameJam Storage: BaseRoom nie je v otvorenej scene. " +
                             "Prefab zostal v " + StoragePrefabPath);
            return;
        }

        Transform container = room.transform.Find("Storage");
        if (container == null)
        {
            GameObject holder = new GameObject("Storage");
            Undo.RegisterCreatedObjectUndo(holder, "Create Storage Group");
            holder.transform.SetParent(room.transform, false);
            container = holder.transform;
        }

        Transform existing = container.Find("BaseStorageChest");
        if (existing != null) return; // zachovaj obsah existujuceho skladu

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(StoragePrefabPath);
        if (prefab == null) return;
        GameObject chest = PrefabUtility.InstantiatePrefab(prefab, container) as GameObject;
        if (chest == null) return;
        Undo.RegisterCreatedObjectUndo(chest, "Place Storage Chest");
        chest.transform.localPosition = new Vector3(-6f, -2.2f, 0f);
        chest.transform.localRotation = Quaternion.identity;
        EditorSceneManager.MarkSceneDirty(room.scene);
    }

    static void AddAnimation(GameObject machine, Sprite gear, Sprite steam, Sprite glow)
    {
        if (machine.GetComponent<FactoryMachine>() == null) return;
        FactoryMachineAnimator animation = machine.GetComponent<FactoryMachineAnimator>();
        if (animation == null) animation = machine.AddComponent<FactoryMachineAnimator>();

        SpriteRenderer body = machine.GetComponent<SpriteRenderer>();
        animation.bodyRenderer = body;
        int baseOrder = body == null ? 6 : body.sortingOrder + 1;
        animation.gearRenderer = EnsureSprite(machine.transform, "FX_RotatingGear", gear,
            new Vector3(0.42f, -0.31f, 0f), baseOrder);
        animation.steamRenderer = EnsureSprite(machine.transform, "FX_Steam", steam,
            new Vector3(-0.32f, 0.74f, 0f), baseOrder + 1);
        animation.glowRenderer = EnsureSprite(machine.transform, "FX_Glow", glow,
            new Vector3(0.08f, -0.48f, 0f), baseOrder + 2);
    }

    static SpriteRenderer EnsureSprite(Transform parent, string name, Sprite sprite, Vector3 pos, int order)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            child = go.transform;
        }
        child.localPosition = pos;
        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = child.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false; // FactoryMachineAnimator ho zapne pocas hry
        return renderer;
    }
}
#endif
