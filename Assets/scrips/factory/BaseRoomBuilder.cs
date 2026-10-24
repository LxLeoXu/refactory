using UnityEngine;

// Vyrobi zakladnu raz v EDITORE. Nemeni dungeon ani RunManager.
public class BaseRoomBuilder : MonoBehaviour
{
    [Header("Velkost miestnosti (rovnaka ako RunManager.baseRoomSize)")]
    public Vector2 size = new Vector2(20f, 12f);
    [Min(0.2f)] public float wallThickness = 1f;
    public bool showStationPads = true;

    const string GeometryName = "_GeneratedBaseGeometry";
    static Sprite whiteSprite;
    Transform geometry;

    [ContextMenu("Build / Refresh Base Room")]
    public void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("BaseRoomBuilder: miestnost generuj mimo Play Mode.");
            return;
        }

        Transform previous = transform.Find(GeometryName);
        if (previous != null) DestroyImmediate(previous.gameObject);

        GameObject root = new GameObject(GeometryName);
        root.transform.SetParent(transform, false);
        geometry = root.transform;

        float w = Mathf.Max(8f, size.x);
        float h = Mathf.Max(8f, size.y);
        float t = Mathf.Clamp(wallThickness, 0.2f, Mathf.Min(w, h) * 0.2f);

        Part("Floor", Vector2.zero, new Vector2(w, h), "floor_moss", -10, false,
            new Color(0.21f, 0.23f, 0.23f));

        Part("Wall_N", new Vector2(0f, h / 2f - t / 2f), new Vector2(w, t),
            "wall_stone", -5, true, Color.gray);
        Part("Wall_S", new Vector2(0f, -h / 2f + t / 2f), new Vector2(w, t),
            "wall_stone", -5, true, Color.gray);
        Part("Wall_E", new Vector2(w / 2f - t / 2f, 0f), new Vector2(t, h),
            "wall_stone", -5, true, Color.gray);
        Part("Wall_W", new Vector2(-w / 2f + t / 2f, 0f), new Vector2(t, h),
            "wall_stone", -5, true, Color.gray);

        if (showStationPads)
        {
            Part("FurnaceSpot", new Vector2(-5f, 2f), new Vector2(2f, 2f),
                "floor_treasure", -9, false, new Color(0.5f, 0.32f, 0.18f));
            Part("AssemblerSpot", new Vector2(0f, 2f), new Vector2(2f, 2f),
                "floor_treasure", -9, false, new Color(0.5f, 0.32f, 0.18f));
            Part("ArmorSpot", new Vector2(5f, 2f), new Vector2(2f, 2f),
                "floor_treasure", -9, false, new Color(0.5f, 0.32f, 0.18f));
        }

        // NIKDY nemazeme rucne vlozene stroje.
        if (transform.Find("Stations") == null)
        {
            var stations = new GameObject("Stations");
            stations.transform.SetParent(transform, false);
        }
        if (transform.Find("BaseSpawn") == null)
        {
            var spawn = new GameObject("BaseSpawn");
            spawn.transform.SetParent(transform, false);
            spawn.transform.localPosition = Vector3.zero;
        }

#if UNITY_EDITOR
        if (gameObject.scene.IsValid())
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        Debug.Log("BASE: hotova miestnost. Teraz vloz stroje pod BaseRoom/Stations.");
    }

    void Part(string partName, Vector2 position, Vector2 partSize,
        string spriteName, int sortingOrder, bool solid, Color fallbackColor)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(geometry, false);
        go.transform.localPosition = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        Sprite sprite = Resources.Load<Sprite>("DungeonTiles/" + spriteName);

        if (sprite != null)
        {
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = partSize;
            sr.color = Color.white;
        }
        else
        {
            if (whiteSprite == null)
            {
                Texture2D tex = Texture2D.whiteTexture;
                whiteSprite = Sprite.Create(tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
            }
            sr.sprite = whiteSprite;
            sr.color = fallbackColor;
            go.transform.localScale = new Vector3(partSize.x, partSize.y, 1f);
        }

        if (solid)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = sprite != null ? partSize : Vector2.one;
        }
    }
}
