using UnityEngine;

public sealed class DungeonRoomContent : MonoBehaviour
{
    public Vector2 RewardPosition { get; private set; }
    public DungeonMazeLayout Maze { get; private set; }
    public float CellSize { get; private set; }
    static Sprite white;
    public void Build(Room room,DungeonRoomKind kind,Vector2 size,DungeonContentProfile profile,int seed,bool traps)
    {
        if(kind==DungeonRoomKind.Maze)
        {
            CellSize=Mathf.Clamp(profile!=null ? profile.mazeCellSize : 2.2f,2f,3f);
            int width=Mathf.Max(5,Mathf.FloorToInt((size.x-4f)/CellSize)); if(width%2==0) width--;
            int height=Mathf.Max(5,Mathf.FloorToInt((size.y-4f)/CellSize)); if(height%2==0) height--;
            Maze=new DungeonMazeLayout(width,height,seed);
            Sprite wall=profile!=null && profile.mazeWall!=null ? profile.mazeWall : RefactorySpriteCatalog.Load("DungeonTiles/wall_stone");
            Color tint=profile!=null ? profile.mazeTint : new Color(0.55f,0.75f,0.85f);
            for(int x=0;x<Maze.width;x++) for(int y=0;y<Maze.height;y++)
                if(!Maze.open[x,y]) Part("MazeWall_"+x+"_"+y,Maze.Position(new Vector2Int(x,y),CellSize),Vector2.one*CellSize,wall,tint,true);
            RewardPosition=Maze.Position(Maze.reward,CellSize);
        }
        else
        {
            RewardPosition=new Vector2(size.x*0.32f,size.y*0.32f);
            if(traps)
            {
                for(int row=-1;row<=1;row++) for(int col=-1;col<=1;col++)
                {
                    if(row==0 && col==0) continue;
                    var position=new Vector2(col*size.x*0.23f,row*size.y*0.23f);
                    var trap=Part("TimedSpikes_"+col+"_"+row,position,new Vector2(2.6f,1.7f),RefactorySpriteCatalog.Load("DungeonTiles/trap_spikes"),Color.white,false);
                    var trigger=trap.AddComponent<BoxCollider2D>(); trigger.isTrigger=true; trigger.size=new Vector2(2.4f,1.5f);
                    var hazard=trap.AddComponent<DungeonSpikeTrap>(); hazard.owner=room; hazard.timed=true;
                    // Visual belongs to a child to keep collider dimensions independent of sprite PPU.
                    hazard.SetVisual(trap.GetComponentInChildren<SpriteRenderer>());
                    hazard.phaseOffset=(row+1)*0.45f+(col+1)*0.18f;
                    if(profile!=null)
                    { hazard.damage=profile.trapDamage; hazard.safeTime=profile.trapSafeTime; hazard.warningTime=profile.trapWarningTime; hazard.activeTime=profile.trapActiveTime; hazard.warningColor=profile.trapWarningColor; }
                }
            }
        }
        Part("RewardMarker",RewardPosition,Vector2.one*1.7f,RefactorySpriteCatalog.Load("DungeonTiles/floor_treasure"),Color.white,false,-7);
    }
    GameObject Part(string name,Vector2 position,Vector2 size,Sprite sprite,Color tint,bool solid,int order=1)
    {
        var root=new GameObject(name); root.transform.SetParent(transform,false); root.transform.localPosition=position;
        var graphic=new GameObject("Visual"); graphic.transform.SetParent(root.transform,false);
        var renderer=graphic.AddComponent<SpriteRenderer>();
        if(sprite==null)
        {
            if(white==null) white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*0.5f,1f);
            sprite=white;
        }
        renderer.sprite=sprite; renderer.color=tint; renderer.sortingOrder=order;
        Vector2 bounds=sprite.bounds.size;
        graphic.transform.localScale=new Vector3(size.x/Mathf.Max(0.001f,bounds.x),size.y/Mathf.Max(0.001f,bounds.y),1f);
        if(solid) root.AddComponent<BoxCollider2D>().size=size;
        return root;
    }
}
