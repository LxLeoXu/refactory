using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DungeonPlayProbe
{
    static int stage, phaseMask;
    static double next, deadline;
    static RunManager manager;
    static DungeonGenerator generator;
    static Room trapRoom, mazeRoom, battleRoom;
    static DungeonSpikeTrap trap;
    static float healthBefore;
    static bool sawWarning, sawDamage;
    static DungeonPlayProbe() { EditorApplication.update+=Tick; }
    static void Require(bool condition,string message) { if(!condition) throw new Exception(message); }
    static void Warp(Vector2 position)
    {
        manager.player.position=position;
        var rb=manager.player.GetComponent<Rigidbody2D>(); if(rb!=null) { rb.position=position; rb.linearVelocity=Vector2.zero; }
        Physics2D.SyncTransforms();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isPaused || EditorApplication.isCompiling || !File.Exists("DungeonPlayProbe.request")) return;
        if(EditorApplication.timeSinceStartup<next) return;
        try
        {
            if(stage==0)
            {
                manager=UnityEngine.Object.FindFirstObjectByType<RunManager>(); generator=UnityEngine.Object.FindFirstObjectByType<DungeonGenerator>();
                Require(manager!=null && generator!=null,"Missing runtime manager/generator");
                next=EditorApplication.timeSinceStartup+1; stage=1; return;
            }
            if(stage==1)
            {
                Require(generator.GeneratedRooms.Count==0,"Dungeon generated prematurely while still in base");
                Require(Vector2.Distance(manager.player.position,manager.baseSpawn.position)<1f,"Player did not start in base");
                generator.seed=20261109; manager.StartRun();
                next=EditorApplication.timeSinceStartup+1; stage=2; return;
            }
            if(stage==2)
            {
                Require(generator.GeneratedRooms.Count==10,"Run did not generate 10 rooms");
                Require(Vector2.Distance(manager.player.position,generator.transform.position)<1f,"StartRun did not teleport player");
                trapRoom=generator.GeneratedRooms.Single(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Trap);
                mazeRoom=generator.GeneratedRooms.Single(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Maze);
                battleRoom=generator.GeneratedRooms.Single(r=>r.GetComponent<DungeonRoomExtras>().kind==DungeonRoomKind.Battle);
                trap=trapRoom.GetComponentsInChildren<DungeonSpikeTrap>().First(t=>Mathf.Abs(t.phaseOffset)<0.01f);
                healthBefore=manager.playerStats.Health; Warp(trap.transform.position);
                deadline=EditorApplication.timeSinceStartup+7; stage=3; return;
            }
            if(stage==3)
            {
                phaseMask|=1<<(int)trap.CurrentPhase;
                if(!sawDamage && trap.CurrentPhase==DungeonSpikeTrap.Phase.Warning)
                { Require(manager.playerStats.Health==healthBefore,"Warning phase dealt damage"); sawWarning=true; }
                if(manager.playerStats.Health<healthBefore) sawDamage=true;
                if(sawWarning && sawDamage && phaseMask==7)
                {
                    Require(trapRoom.HasEntered && trapRoom.IsCleared,"Traversal room did not enter/clear without combat lock");
                    Require(trapRoom.GetComponentsInChildren<LootChest>().Length==1,"Trap reward missing or duplicated");
                    var content=mazeRoom.GetComponent<DungeonRoomContent>();
                    Warp(mazeRoom.transform.TransformPoint(content.Maze.Position(content.Maze.portals[0],content.CellSize)));
                    next=EditorApplication.timeSinceStartup+0.8; stage=4; return;
                }
                Require(EditorApplication.timeSinceStartup<deadline,"Trap did not complete safe/warning/damage sequence"); return;
            }
            if(stage==4)
            {
                var content=mazeRoom.GetComponent<DungeonRoomContent>();
                Require(mazeRoom.HasEntered && mazeRoom.IsCleared,"Maze entry blocked");
                var chest=mazeRoom.GetComponentInChildren<LootChest>(); Require(chest!=null,"Maze reward missing");
                Require(Vector2.Distance(chest.transform.localPosition,content.RewardPosition)<0.01f,"Maze reward off passage");
                Require(chest.GetComponentsInChildren<Collider2D>().All(c=>c.isTrigger),"Maze chest can block route");
                Require(chest.rewardScatterRadius<=0.3f,"Maze loot can scatter into walls");
                chest.Open();
                var pickups=mazeRoom.GetComponentsInChildren<ItemPickup>(); Require(pickups.Length>=2,"Maze chest produced no treasure");
                foreach(var pickup in pickups) Require(Vector2.Distance(pickup.transform.localPosition,content.RewardPosition)<0.4f,"Maze loot spawned outside reward cell");
                Warp((Vector2)battleRoom.transform.position+new Vector2(0,-battleRoom.Size.y*0.5f+2.4f));
                next=EditorApplication.timeSinceStartup+0.3; stage=5; return;
            }
            if(stage==5)
            {
                Require(battleRoom.HasEntered,"Battle room not entered");
                Require(battleRoom.GetComponentsInChildren<Enemy>().Length>0,"Battle room has no valid enemies");
                File.WriteAllText("DUNGEON_PLAYTEST.txt","PASS\nBase startup waits for StartRun.\nStartRun teleports player and generates 10 rooms.\nTrap: safe + warning + active phases observed, warning causes no damage, active phase damages player.\nTrap reward spawned once; doors remain open.\nMaze entry works; reward is on a reachable cell, chest is non-blocking; 2+ loot drops stay inside reward cell.\nBattle room spawns surviving valid enemy references without null-instantiation errors.\n");
                File.Delete("DungeonPlayProbe.request");
                // Leave the live camera on the new maze for visual review; stop through the editor afterward.
                var content=mazeRoom.GetComponent<DungeonRoomContent>(); Warp(mazeRoom.transform.TransformPoint(content.RewardPosition)); mazeRoom.ApplyCameraBounds();
                Debug.Log("REFACTORY Dungeon V4 PLAYTEST PASS");
            }
        }
        catch(Exception e)
        {
            File.WriteAllText("DUNGEON_PLAYTEST.txt",e.ToString()); File.Delete("DungeonPlayProbe.request"); Debug.LogException(e);
        }
    }
}
