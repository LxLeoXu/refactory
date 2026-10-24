#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Len pre import NOVYCH textur. Nepridava duplikat ziadneho herneho skriptu.
public static class GameJamDungeonV2Art
{
    [MenuItem("Tools/GameJam/Import Dungeon V2 Art")]
    public static void ImportNewSprites()
    {
        string[] spriteNames = {
            "floor_ruins", "floor_supply", "floor_ambush",
            "prop_crate", "prop_pillar", "trap_spikes", "prop_lamp"
        };
        int imported = 0;
        foreach (string name in spriteNames)
        {
            string path = "Assets/Resources/DungeonTiles/" + name + ".png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("Dungeon V2: nenasiel som " + path);
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            imported++;
        }
        AssetDatabase.Refresh();
        Debug.Log("Dungeon V2: nastavene " + imported + "/" + spriteNames.Length + " sprite assety.");
    }
}
#endif
