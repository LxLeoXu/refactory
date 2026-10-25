#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Editor script: menu Tools > GameJam.
public static class GameJamFactorySetup
{
    const string Folder = "Assets/GameJamFactory";
    const string Art = Folder + "/Sprites/";
    const string Prefabs = Folder + "/Prefabs/";

    class Config
    {
        public string name, image, recipe;
        public float seconds, x;
        public Config(string name, string image, string recipe, float seconds, float x)
        { this.name=name; this.image=image; this.recipe=recipe; this.seconds=seconds; this.x=x; }
    }

    static readonly Config[] Machines = {
        new Config("Furnace", "Machine_Furnace", "metal_plate_recipe", 5f, -5f),
        new Config("Assembler", "Machine_Assembler", "weapon_part_recipe", 8f, 0f),
        new Config("ArmorPress", "Machine_ArmorPress", "armor_plate_recipe", 10f, 5f),
        new Config("UpgradeStation", "Machine_UpgradeStation", "weapon_upgrade_recipe", 12f, 0f)
    };

    [MenuItem("Tools/GameJam/Generate Factory Prefabs")]
    public static void GeneratePrefabs()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            Debug.LogError("Missing folder: " + Folder + ". Copy the complete ZIP Assets folder.");
            return;
        }
        if (!AssetDatabase.IsValidFolder(Prefabs))
            AssetDatabase.CreateFolder(Folder, "Prefabs");
        int created = 0;
        foreach (Config spec in Machines)
        {
            string path = Prefabs + "Factory_" + spec.name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("Keeping existing prefab: " + path);
                continue;
            }

            Sprite sprite = ImportSprite(Art + spec.image + ".png", 32f);
            Sprite light = ImportSprite(Art + "Machine_StatusLamp.png", 32f);
            if (sprite == null || light == null) continue;

            GameObject obj = new GameObject("Factory_" + spec.name);
            try
            {
                SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 3;
                BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
                box.size = new Vector2(1.53f, 1.57f);
                box.offset = new Vector2(0f, -0.05f);
                FactoryMachine machine = obj.AddComponent<FactoryMachine>();
                machine.machineName = spec.name;
                machine.recipeId = spec.recipe;
                machine.productionTime = spec.seconds;
                machine.interactionRange = 2.3f;

                GameObject lampGo = new GameObject("StatusLamp");
                lampGo.transform.SetParent(obj.transform, false);
                lampGo.transform.localPosition = new Vector3(0.66f, 0.67f, 0f);
                SpriteRenderer lampSr = lampGo.AddComponent<SpriteRenderer>();
                lampSr.sprite = light;
                lampSr.sortingOrder = 4;
                FactoryMachineLight visual = obj.AddComponent<FactoryMachineLight>();
                visual.statusLamp = lampSr;

                PrefabUtility.SaveAsPrefabAsset(obj, path);
                created++;
            }
            finally { Object.DestroyImmediate(obj); }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Factory prefabs created: " + created + ". Assets/GameJamFactory/Prefabs");
    }

    static Sprite ImportSprite(string path, float ppu)
    {
        TextureImporter t = AssetImporter.GetAtPath(path) as TextureImporter;
        if (t == null)
        {
            Debug.LogError("Factory sprite not found: " + path);
            return null;
        }
        if (t.textureType != TextureImporterType.Sprite || t.spritePixelsPerUnit != ppu ||
            t.filterMode != FilterMode.Point || t.textureCompression != TextureImporterCompression.Uncompressed)
        {
            t.textureType = TextureImporterType.Sprite;
            t.spriteImportMode = SpriteImportMode.Single;
            t.spritePixelsPerUnit = ppu;
            t.filterMode = FilterMode.Point;
            t.textureCompression = TextureImporterCompression.Uncompressed;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/GameJam/Place Factory Machines In Base")]
    public static void PlaceMachines()
    {
        GameObject baseRoom = GameObject.Find("BaseRoom");
        if (baseRoom == null)
        {
            Debug.LogWarning("Create a GameObject named BaseRoom first. No machines placed.");
            return;
        }
        Transform stations = baseRoom.transform.Find("Stations");
        if (stations == null)
        {
            GameObject go = new GameObject("Stations");
            Undo.RegisterCreatedObjectUndo(go, "Create Stations");
            go.transform.SetParent(baseRoom.transform, false);
            stations = go.transform;
        }
        for (int i = 0; i < Machines.Length; i++)
        {
            Config spec = Machines[i];
            string name = "Factory_" + spec.name;
            if (stations.Find(name) != null) continue;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
            if (prefab == null)
            {
                Debug.LogError("Prefab missing: " + name + ". Run Generate Factory Prefabs first.");
                continue;
            }
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab, stations) as GameObject;
            if (obj == null) continue;
            Undo.RegisterCreatedObjectUndo(obj, "Place Factory Machines");
            obj.transform.localPosition = new Vector3(spec.x, i == 3 ? -2.2f : 2f, 0f);
            obj.transform.localRotation = Quaternion.identity;
        }
        EditorSceneManager.MarkSceneDirty(baseRoom.scene);
        Debug.Log("Placed factory machines under BaseRoom/Stations.");
    }
}
#endif
