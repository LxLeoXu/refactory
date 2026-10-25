#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Only an EDITOR tool. Doesn't replace BaseRoomBuilder, FactoryMachine,
// BaseStorageChest, RunManager or NPC scripts.
public static class GameJamBasePolish
{
    private const string DecorRootName = "_BasePolishDecor";
    private const string SpriteFolder = "Assets/GeneratedBasePolish/Sprites";

    [MenuItem("Tools/GameJam/Base/1 - Add or Refresh Decorations")]
    public static void Decorate()
    {
        Build(false);
    }

    [MenuItem("Tools/GameJam/Base/2 - Tidy Stations and Decorate")]
    public static void TidyAndDecorate()
    {
        if (!EditorUtility.DisplayDialog("Upratat zakladnu?",
            "Presuniem len 4 zname factory stroje, BaseStorageChest a NPC_Echo do prehladneho rozlozenia. " +
            "Rucne postavene veci ani ich komponenty nevymazem. Mozes pouzit Ctrl+Z.",
            "Upratat", "Zrusit")) return;
        Build(true);
    }

    private static void Build(bool moveThings)
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("Base polish: Spusti nastroj mimo Play Mode.");
            return;
        }

        GameObject room = GameObject.Find("BaseRoom");
        if (room == null)
        {
            Debug.LogError("Base polish: V Hierarchy chyba objekt BaseRoom.");
            return;
        }

        // Prepare REAL PNG assets. Prefabs don't reference temporary in-memory sprites.
        Directory.CreateDirectory(SpriteFolder);
        AssetDatabase.Refresh();
        Sprite floor = LoadOrCreate("factory_floor", "floor");
        Sprite aisle = LoadOrCreate("walkway", "aisle");
        Sprite hazard = LoadOrCreate("warning_stripes", "hazard");
        Sprite pad = LoadOrCreate("machine_pad", "pad");
        Sprite crate = LoadOrCreate("factory_crate", "crate");
        Sprite lamp = LoadOrCreate("amber_lamp", "lamp");
        Sprite terminal = LoadOrCreate("wall_terminal", "terminal");
        if (floor == null || aisle == null || hazard == null || pad == null ||
            crate == null || lamp == null || terminal == null)
        {
            Debug.LogError("Base polish: Nepodarilo sa pripravit vsetky sprity.");
            return;
        }

        // IMPORTANT: Only delete our OWN generated visuals.
        Transform previous = room.transform.Find(DecorRootName);
        if (previous != null) Undo.DestroyObjectImmediate(previous.gameObject);

        Transform decor = Group(room.transform, DecorRootName);
        Transform floors = Group(decor, "01_Floor_Overlays");
        Transform zones = Group(decor, "02_Work_Zones");
        Transform props = Group(decor, "03_Decorative_Props");
        Transform lights = Group(decor, "04_Lights");

        // Designed for the existing BaseRoomBuilder size 20 x 12.
        // No colliders on overlays or decorative props; movement stays unobstructed.
        Image(floors, "Clean_Metal_Floor", floor, 0f, 0f, 18f, 10f, -8, Color.white);
        Image(floors, "Central_Walkway", aisle, 0f, 0f, 15.5f, 1.9f, -7,
            new Color(0.92f, 0.94f, 1f, 1f));
        Image(floors, "To_Workstations", aisle, 0f, 1.1f, 1.9f, 2.25f, -7,
            new Color(0.92f, 0.94f, 1f, 1f));
        Image(floors, "To_Upgrade", aisle, 0f, -1.25f, 1.9f, 0.9f, -7,
            new Color(0.92f, 0.94f, 1f, 1f));
        Image(zones, "North_Safety_Band", hazard, 0f, 4.1f, 15.5f, 0.25f, -6, Color.white);
        Image(zones, "South_Safety_Band", hazard, 0f, -4.1f, 15.5f, 0.25f, -6, Color.white);

        WorkZone(zones, pad, hazard, "Furnace", -5f, 2f, new Color(1f, 0.74f, 0.48f, 1f));
        WorkZone(zones, pad, hazard, "Assembler", 0f, 2f, new Color(0.59f, 0.81f, 1f, 1f));
        WorkZone(zones, pad, hazard, "ArmorPress", 5f, 2f, new Color(0.62f, 1f, 0.78f, 1f));
        WorkZone(zones, pad, hazard, "UpgradeStation", 0f, -2.2f,
            new Color(0.83f, 0.71f, 1f, 1f));
        WorkZone(zones, pad, hazard, "Storage", -6f, -2.2f,
            new Color(0.94f, 0.81f, 0.51f, 1f));
        WorkZone(zones, pad, hazard, "QuietCorner", 6f, -2.2f,
            new Color(0.61f, 0.83f, 1f, 1f));

        // Decorative assets live near the walls; no gameplay colliders.
        Image(props, "Crates_NW", crate, -8.1f, 3.7f, 1f, 1f, -3, Color.white);
        Image(props, "Crates_NE", crate, 8.1f, 3.7f, 1f, 1f, -3, Color.white);
        Image(props, "Crates_SW", crate, -8.1f, -3.75f, 1f, 1f, -3, Color.white);
        Image(props, "Terminal_E", terminal, 8.15f, 0.6f, 1f, 1f, -3, Color.white);
        Image(props, "Terminal_W", terminal, -8.15f, 0.6f, 1f, 1f, -3, Color.white);
        Image(lights, "Lamp_NW", lamp, -7.65f, 4.4f, 0.75f, 0.75f, -3, Color.white);
        Image(lights, "Lamp_NE", lamp, 7.65f, 4.4f, 0.75f, 0.75f, -3, Color.white);
        Image(lights, "Lamp_SW", lamp, -7.65f, -4.4f, 0.75f, 0.75f, -3, Color.white);
        Image(lights, "Lamp_SE", lamp, 7.65f, -4.4f, 0.75f, 0.75f, -3, Color.white);

        int moved = moveThings ? TidyExistingObjects(room.transform) : 0;
        EditorSceneManager.MarkSceneDirty(room.scene);
        Selection.activeGameObject = room;
        Debug.Log("GameJam BASE: Dekoracie obnovene. Presunute existujuce objekty: " + moved +
            ". Vsetko ostatne zostalo bez zmeny.");
    }

    private static void WorkZone(Transform parent, Sprite pad, Sprite hazard,
        string name, float x, float y, Color tint)
    {
        Image(parent, name + "_Pad", pad, x, y, 2.7f, 2.55f, -6, tint);
        Image(parent, name + "_SafetyStripe", hazard, x, y - 1.23f,
            2.7f, 0.17f, -5, Color.white);
    }

    // This is OPTIONAL. Does not change parent, references, components, or the base spawn.
    private static int TidyExistingObjects(Transform root)
    {
        int count = 0;
        count += Position(root, "Factory_Furnace", -5f, 2f);
        count += Position(root, "Factory_Assembler", 0f, 2f);
        count += Position(root, "Factory_ArmorPress", 5f, 2f);
        count += Position(root, "Factory_UpgradeStation", 0f, -2.2f);
        count += Position(root, "BaseStorageChest", -6f, -2.2f);
        count += Position(root, "NPC_Echo", 6f, -2.2f);
        return count;
    }

    private static int Position(Transform baseRoom, string name, float localX, float localY)
    {
        Transform objectToMove = null;
        foreach (Transform t in baseRoom.GetComponentsInChildren<Transform>(true))
        {
            if (t == baseRoom || t.name != name) continue;
            objectToMove = t;
            break;
        }
        if (objectToMove == null) return 0;

        Undo.RecordObject(objectToMove, "Tidy " + name);
        Vector3 target = baseRoom.TransformPoint(new Vector3(localX, localY, 0f));
        target.z = objectToMove.position.z;
        objectToMove.position = target;
        PrefabUtility.RecordPrefabInstancePropertyModifications(objectToMove);
        return 1;
    }

    private static Transform Group(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Build Base Decorations");
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static void Image(Transform parent, string name, Sprite sprite,
        float x, float y, float width, float height, int order, Color tint)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Add Base Decoration");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(width, height);
        sr.sortingOrder = order;
        sr.color = tint;
    }

    private static Sprite LoadOrCreate(string name, string kind)
    {
        string path = SpriteFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Texture2D tex = CreateTexture(kind);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        // Unity 6: spriteMeshType je v TextureImporterSettings, nie v TextureImporter.
        bool changeImporter = importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.filterMode != FilterMode.Point ||
            importer.mipmapEnabled || importer.spritePixelsPerUnit != 32f ||
            importer.textureCompression != TextureImporterCompression.Uncompressed;

        if (changeImporter)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool changeMesh = settings.spriteMeshType != SpriteMeshType.FullRect;
        if (changeMesh)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        if (changeImporter || changeMesh)
            importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Texture2D CreateTexture(string kind)
    {
        const int n = 32;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32 dark = C(32, 37, 47), steel = C(63, 72, 88), edge = C(89, 100, 117);
        Color32 brass = C(227, 157, 43), glow = C(255, 203, 83);
        Color32[] p = new Color32[n * n];
        Color32 background = (kind == "crate" || kind == "lamp" || kind == "terminal")
            ? new Color32(0, 0, 0, 0) : dark;
        for (int i = 0; i < p.Length; i++) p[i] = background;

        if (kind == "floor" || kind == "aisle" || kind == "pad")
        {
            Fill(p, 1, 1, 30, 30, kind == "pad" ? C(41, 52, 65) : steel);
            Fill(p, 0, 0, 31, 1, edge); Fill(p, 0, 30, 31, 31, dark);
            Fill(p, 0, 0, 1, 31, edge); Fill(p, 30, 0, 31, 31, dark);
            Fill(p, 4, 4, 5, 5, C(120, 131, 148));
            Fill(p, 26, 26, 27, 27, C(120, 131, 148));
            if (kind == "aisle")
            {
                Fill(p, 10, 13, 21, 18, C(73, 94, 107));
                Fill(p, 13, 14, 18, 17, C(135, 161, 170));
            }
            if (kind == "pad")
            {
                Fill(p, 3, 3, 28, 3, brass); Fill(p, 3, 28, 28, 28, brass);
                Fill(p, 3, 4, 3, 27, brass); Fill(p, 28, 4, 28, 27, brass);
            }
        }
        else if (kind == "hazard")
        {
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    p[y * n + x] = ((x + y) % 16 < 8) ? C(232, 169, 51) : C(26, 29, 34);
        }
        else if (kind == "crate")
        {
            Fill(p, 3, 3, 28, 28, dark);
            Fill(p, 5, 5, 26, 26, C(132, 84, 51));
            Fill(p, 7, 7, 24, 24, C(171, 110, 64));
            Fill(p, 13, 7, 17, 24, C(91, 62, 49));
            Fill(p, 7, 13, 24, 17, C(91, 62, 49));
            Fill(p, 5, 5, 7, 7, edge); Fill(p, 24, 24, 26, 26, edge);
        }
        else if (kind == "lamp")
        {
            Fill(p, 7, 7, 24, 24, dark);
            Fill(p, 10, 10, 21, 21, brass);
            Fill(p, 12, 12, 19, 19, glow);
            Fill(p, 14, 14, 17, 17, C(255, 241, 194));
            Fill(p, 7, 7, 9, 9, edge); Fill(p, 22, 22, 24, 24, edge);
        }
        else if (kind == "terminal")
        {
            Fill(p, 4, 3, 27, 28, dark);
            Fill(p, 7, 6, 24, 19, C(17, 69, 87));
            Fill(p, 9, 9, 20, 10, C(62, 194, 210));
            Fill(p, 9, 13, 18, 14, C(62, 194, 210));
            Fill(p, 10, 23, 13, 25, brass);
            Fill(p, 17, 23, 20, 25, edge);
        }
        tex.SetPixels32(p);
        tex.Apply();
        return tex;
    }

    private static Color32 C(byte r, byte g, byte b) => new Color32(r, g, b, 255);

    private static void Fill(Color32[] p, int x1, int y1, int x2, int y2, Color32 color)
    {
        for (int y = Math.Max(0, y1); y <= Math.Min(31, y2); y++)
            for (int x = Math.Max(0, x1); x <= Math.Min(31, x2); x++)
                p[y * 32 + x] = color;
    }
}
#endif
