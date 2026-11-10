using System;
using UnityEngine;

[CreateAssetMenu(menuName="REFACTORY/Factory Layout Definition")]
public sealed class FactoryLayoutDefinition : ScriptableObject
{
    public enum ZoneRole { Smelting, Assembly, Upgrades, Storage, Expedition, Expansion }
    [Serializable] public struct StationSlot
    {
        public string id;
        public ZoneRole role;
        public Vector2 position;
        public Vector2 footprint;
        [Tooltip("Reserved for the future machine placement system; preview never starts production.")]
        public GameObject machinePrefab;
    }
    public Vector2 roomSize=new Vector2(28f,18f);
    public Vector2 playerSpawn=new Vector2(0f,-5f);
    [Min(1f)] public float centralAisleWidth=4f;
    public Sprite floorSprite, wallSprite, padSprite;
    public StationSlot[] stations={
        new StationSlot{id="smelting",role=ZoneRole.Smelting,position=new Vector2(-8f,4f),footprint=new Vector2(4f,4f)},
        new StationSlot{id="assembly",role=ZoneRole.Assembly,position=new Vector2(0f,4f),footprint=new Vector2(4f,4f)},
        new StationSlot{id="upgrades",role=ZoneRole.Upgrades,position=new Vector2(8f,4f),footprint=new Vector2(4f,4f)},
        new StationSlot{id="storage",role=ZoneRole.Storage,position=new Vector2(-8f,-4f),footprint=new Vector2(4f,3f)},
        new StationSlot{id="expedition",role=ZoneRole.Expedition,position=new Vector2(8f,-4f),footprint=new Vector2(4f,3f)}
    };
}
