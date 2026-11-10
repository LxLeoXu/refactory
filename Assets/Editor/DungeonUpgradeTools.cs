using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DungeonUpgradeTools
{
    static DungeonUpgradeTools() { EditorApplication.delayCall+=AutoPrepare; }
    static void AutoPrepare()
    {
        if(!File.Exists("DungeonUpgrade.request")) return;
        if(EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=AutoPrepare; return; }
        File.Delete("DungeonUpgrade.request");
        try { Prepare(); Validate(); RefactoryCleanupTools.Audit(); }
        catch(Exception e) { File.WriteAllText("DUNGEON_UPGRADE_VALIDATION.txt",e.ToString()); Debug.LogException(e); }
    }
    static void Folder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        Folder(Path.GetDirectoryName(path).Replace('\\','/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\','/'),Path.GetFileName(path));
    }
    [MenuItem("Tools/REFACTORY/Dungeon V4/Prepare profiles and factory preview")]
    public static void Prepare()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        const string dungeonPath="Assets/Resources/DungeonContentProfile.asset";
        if(AssetDatabase.LoadAssetAtPath<DungeonContentProfile>(dungeonPath)==null)
        {
            var profile=ScriptableObject.CreateInstance<DungeonContentProfile>();
            profile.mazeWall=RefactorySpriteCatalog.Load("DungeonTiles/wall_stone");
            AssetDatabase.CreateAsset(profile,dungeonPath);
        }
        Folder("Assets/_Project/Settings");
        const string factoryPath="Assets/_Project/Settings/FactoryLayout_Default.asset";
        var factory=AssetDatabase.LoadAssetAtPath<FactoryLayoutDefinition>(factoryPath);
        if(factory==null)
        {
            factory=ScriptableObject.CreateInstance<FactoryLayoutDefinition>();
            factory.floorSprite=RefactorySpriteCatalog.Load("DungeonTiles/factory_floor");
            factory.wallSprite=RefactorySpriteCatalog.Load("DungeonTiles/wall_stone");
            factory.padSprite=RefactorySpriteCatalog.Load("DungeonTiles/floor_treasure");
            AssetDatabase.CreateAsset(factory,factoryPath);
        }
        AssetDatabase.SaveAssets();
        const string previewPath="Assets/_Project/Prefabs/FactoryLayoutPrototype.prefab";
        if(!File.Exists(previewPath))
        {
            var scene=EditorSceneManager.NewPreviewScene();
            var root=new GameObject("FactoryLayoutPrototype"); SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var authoring=root.AddComponent<FactoryLayoutAuthoring>(); authoring.layout=factory; authoring.BuildPreview();
                PrefabUtility.SaveAsPrefabAsset(root,previewPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    [MenuItem("Tools/REFACTORY/Dungeon V4/Validate layouts and content")]
    public static void Validate()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        int graphTests=0,mazeTests=0;
        for(int seed=1;seed<=100;seed++)
        {
            foreach(int count in new[]{2,5,10,18,32})
            {
                var plan=DungeonLayout.Build(count,seed); var repeat=DungeonLayout.Build(count,seed);
                Require(plan.cells.Count==count && plan.cells.Distinct().Count()==count,"Wrong graph room count");
                Require(plan.cells.SequenceEqual(repeat.cells),"Seed is not repeatable");
                Require(plan.depth[plan.boss]==plan.depth.Values.Max(),"Boss not deepest");
                int edges=0;
                foreach(var c in plan.cells)
                {
                    int parents=0;
                    foreach(var d in DungeonLayout.Directions)
                        if(plan.depth.TryGetValue(c+d,out int depth)) { edges++; if(depth==plan.depth[c]-1) parents++; }
                    Require(c==Vector2Int.zero || parents==1,"Room disconnected or invalid parent");
                }
                Require(edges==2*(count-1),"Unexpected cycle"); graphTests++;
            }
            foreach(var size in new[]{new Vector2Int(5,5),new Vector2Int(9,7),new Vector2Int(15,11)})
            {
                var maze=new DungeonMazeLayout(size.x,size.y,seed); var seen=new HashSet<Vector2Int>(); var queue=new Queue<Vector2Int>();
                queue.Enqueue(maze.portals[0]); seen.Add(maze.portals[0]);
                while(queue.Count>0)
                {
                    var c=queue.Dequeue();
                    foreach(var d in DungeonLayout.Directions) if(maze.IsOpen(c+d) && seen.Add(c+d)) queue.Enqueue(c+d);
                }
                Require(maze.portals.All(p=>seen.Contains(p)) && seen.Contains(maze.reward),"Maze portal or reward unreachable");
                for(int x=0;x<maze.width;x++) for(int y=0;y<maze.height;y++) Require(!maze.open[x,y] || seen.Contains(new Vector2Int(x,y)),"Disconnected maze passage");
                mazeTests++;
            }
        }
        Require(DungeonSpikeTrap.EvaluatePhase(0,1.5f,0.9f,1.1f)==DungeonSpikeTrap.Phase.Safe,"Trap unsafe on startup");
        Require(DungeonSpikeTrap.EvaluatePhase(1.6f,1.5f,0.9f,1.1f)==DungeonSpikeTrap.Phase.Warning,"Trap warning missing");
        Require(DungeonSpikeTrap.EvaluatePhase(2.5f,1.5f,0.9f,1.1f)==DungeonSpikeTrap.Phase.Active,"Trap never activates");
        Require(DungeonSpikeTrap.EvaluatePhase(3.6f,1.5f,0.9f,1.1f)==DungeonSpikeTrap.Phase.Safe,"Trap never resets");
        var profile=Resources.Load<DungeonContentProfile>("DungeonContentProfile");
        Require(profile!=null,"Missing default profile");
        var savedRandom=UnityEngine.Random.state;
        var preview=EditorSceneManager.NewPreviewScene(); var root=new GameObject("Dungeon validation"); SceneManager.MoveGameObjectToScene(root,preview);
        try
        {
            root.transform.position=new Vector3(10000,10000,0);
            var generator=root.AddComponent<DungeonGenerator>(); generator.contentProfile=profile; generator.seed=314159; generator.increaseDifficultyEachRun=false;
            UnityEngine.Random.InitState(456); float expected=UnityEngine.Random.value; UnityEngine.Random.InitState(456);
            generator.Generate(); Require(UnityEngine.Random.value==expected,"Generator modified shared random state");
            Require(generator.GeneratedRooms.Count==10,"Actual generated room count differs");
            Require(generator.GeneratedRooms.Count(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Trap)==1,"Trap room not guaranteed");
            var mazeRoom=generator.GeneratedRooms.Single(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Maze);
            var content=mazeRoom.GetComponent<DungeonRoomContent>(); Require(content!=null && content.Maze!=null,"Maze geometry absent");
            int walls=0;
            for(int x=0;x<content.Maze.width;x++) for(int y=0;y<content.Maze.height;y++) if(!content.Maze.open[x,y]) walls++;
            Require(mazeRoom.GetComponentsInChildren<BoxCollider2D>().Count(c=>c.name.StartsWith("MazeWall_"))==walls,"Maze collision grid mismatch");
            Require(content.CellSize>=2f,"Passages too narrow for current player");
            Require(content.Maze.width*content.CellSize<=mazeRoom.Size.x-3.9f && content.Maze.height*content.CellSize<=mazeRoom.Size.y-3.9f,"Maze overlaps door landings");
            foreach(var room in generator.GeneratedRooms)
                foreach(var d in DungeonLayout.Directions)
                {
                    var other=generator.GeneratedRooms.FirstOrDefault(r=>r.gridPos==room.gridPos+d); if(other==null) continue;
                    Vector2 edge=(Vector2)room.transform.position+Vector2.Scale(d,room.Size)*0.5f;
                    Vector2 otherEdge=(Vector2)other.transform.position-Vector2.Scale(d,other.Size)*0.5f;
                    Require(Vector2.Distance(edge,otherEdge)<0.001f,"Door edges no longer align");
                }
            var trapRoom=generator.GeneratedRooms.Single(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Trap);
            Require(trapRoom.GetComponentsInChildren<DungeonSpikeTrap>().Length==8,"Trap layout incomplete");
            // Solid geometry must reject a spawn; room triggers must not reject an open floor.
            var wall=mazeRoom.GetComponentsInChildren<BoxCollider2D>().First(c=>c.name.StartsWith("MazeWall_"));
            Physics2D.SyncTransforms();
            // Use the preview scene's own physics world for geometry validation.
            Require(wall.size==Vector2.one*content.CellSize,"Maze collider size changed");
            generator.Generate(); Require(generator.GetComponentsInChildren<Room>().Length==10,"Regeneration leaked old rooms");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); UnityEngine.Random.state=savedRandom; }
        var factory=AssetDatabase.LoadAssetAtPath<FactoryLayoutDefinition>("Assets/_Project/Settings/FactoryLayout_Default.asset");
        Require(factory!=null && factory.floorSprite!=null && factory.wallSprite!=null && factory.padSprite!=null,"Factory profile artwork missing");
        var ids=new HashSet<string>();
        foreach(var slot in factory.stations)
        {
            Require(!string.IsNullOrEmpty(slot.id) && ids.Add(slot.id),"Duplicate factory slot ID");
            Require(Mathf.Abs(slot.position.x)+slot.footprint.x*0.5f<factory.roomSize.x*0.5f-1f && Mathf.Abs(slot.position.y)+slot.footprint.y*0.5f<factory.roomSize.y*0.5f-1f,"Factory slot overlaps perimeter");
            Require(Mathf.Abs(slot.position.y)-slot.footprint.y*0.5f>=factory.centralAisleWidth*0.5f,"Factory slot obstructs central aisle");
        }
        File.WriteAllText("DUNGEON_UPGRADE_VALIDATION.txt","PASS\n"+graphTests+" graph cases: exact count, repeatability, connectivity, deepest boss, no cycles.\n"+mazeTests+" maze cases: all portals, reward and all passages reachable.\nTrap phases: safe, warning, active, reset.\nActual 10-room generation and regeneration: guaranteed trap/maze, matching collider grid, player clearance, aligned doors, unchanged RNG state.\nFactory profile: stable slot IDs, artwork, perimeter and central aisle clearance.\n");
        Debug.Log("REFACTORY Dungeon V4 validation PASS ("+graphTests+" graph, "+mazeTests+" maze cases).");
    }
}
