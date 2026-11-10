using UnityEngine;

// Authoring only: generated markers are isolated from the production base.
// Slot IDs and prefab references are the extension point for later placement,
// unlocking, upgrades and save data. No inventory or production changes here.
public sealed class FactoryLayoutAuthoring : MonoBehaviour
{
    public FactoryLayoutDefinition layout;
    const string PreviewRoot="_LayoutPreview";
    static Sprite white;
    [ContextMenu("Rebuild Factory Layout Preview")]
    public void BuildPreview()
    {
        if(Application.isPlaying || layout==null) return;
        var old=transform.Find(PreviewRoot);
        if(old!=null)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);
#else
            DestroyImmediate(old.gameObject);
#endif
        }
        var root=new GameObject(PreviewRoot); root.transform.SetParent(transform,false);
#if UNITY_EDITOR
        UnityEditor.Undo.RegisterCreatedObjectUndo(root,"Rebuild factory layout preview");
#endif
        Vector2 size=new Vector2(Mathf.Max(12f,layout.roomSize.x),Mathf.Max(12f,layout.roomSize.y));
        Part(root.transform,"Floor",Vector2.zero,size,layout.floorSprite,new Color(0.2f,0.24f,0.28f),-10,false);
        Part(root.transform,"CentralAisle",Vector2.zero,new Vector2(size.x-2f,Mathf.Clamp(layout.centralAisleWidth,2f,size.y-2f)),layout.padSprite,new Color(0.45f,0.65f,0.7f),-9,false);
        for(int sign=-1;sign<=1;sign+=2)
        {
            Part(root.transform,"Wall_NS",new Vector2(0f,sign*(size.y-1f)*0.5f),new Vector2(size.x,1f),layout.wallSprite,Color.gray,-5,true);
            Part(root.transform,"Wall_EW",new Vector2(sign*(size.x-1f)*0.5f,0f),new Vector2(1f,size.y),layout.wallSprite,Color.gray,-5,true);
        }
        foreach(var slot in layout.stations)
        {
            Color tint=slot.role==FactoryLayoutDefinition.ZoneRole.Expedition ? new Color(0.3f,0.8f,1f)
                : slot.role==FactoryLayoutDefinition.ZoneRole.Storage ? new Color(0.6f,0.8f,0.4f) : new Color(1f,0.72f,0.3f);
            Part(root.transform,"Slot_"+slot.id,slot.position,slot.footprint,layout.padSprite,tint,-8,false);
        }
        Part(root.transform,"PlayerSpawn",layout.playerSpawn,Vector2.one,layout.padSprite,Color.green,-7,false);
#if UNITY_EDITOR
        if(gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }
    static void Part(Transform parent,string name,Vector2 position,Vector2 size,Sprite sprite,Color color,int order,bool solid)
    {
        var root=new GameObject(name); root.transform.SetParent(parent,false); root.transform.localPosition=position;
        var graphic=new GameObject("Visual"); graphic.transform.SetParent(root.transform,false);
        var sr=graphic.AddComponent<SpriteRenderer>();
        if(sprite==null)
        {
            if(white==null) white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*0.5f,1f);
            sprite=white;
        }
        sr.sprite=sprite; sr.color=color; sr.sortingOrder=order;
        // Tiled visuals keep the pixel scale independent from the room footprint.
        sr.drawMode=SpriteDrawMode.Tiled; sr.size=size;
        if(solid) root.AddComponent<BoxCollider2D>().size=size;
    }
    void OnDrawGizmosSelected()
    {
        if(layout==null) return;
        Gizmos.matrix=transform.localToWorldMatrix;
        Gizmos.color=Color.cyan; Gizmos.DrawWireCube(Vector3.zero,layout.roomSize);
        foreach(var slot in layout.stations)
        {
            Gizmos.DrawWireCube(slot.position,slot.footprint);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.TransformPoint(slot.position),slot.id+" ("+slot.role+")");
#endif
        }
        Gizmos.matrix=Matrix4x4.identity;
    }
}
