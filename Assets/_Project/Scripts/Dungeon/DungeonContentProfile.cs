using UnityEngine;

[CreateAssetMenu(menuName="REFACTORY/Dungeon Content Profile")]
public sealed class DungeonContentProfile : ScriptableObject
{
    [Header("Traversal rooms - keep doors open, no enemies without maze pathfinding")]
    public bool enableTrapRooms = true;
    public bool enableMazeRooms = true;
    [Range(0f,1f)] public float trapWeight = 0.18f;
    [Range(0f,1f)] public float mazeWeight = 0.14f;
    public Vector2 minimumMazeSize = new Vector2(26f,20f);
    [Min(2f)] public float mazeCellSize = 2.2f;
    [Header("Trap timing (seconds)")]
    [Min(0.3f)] public float trapSafeTime = 1.5f;
    [Min(0.4f)] public float trapWarningTime = 0.9f;
    [Min(0.2f)] public float trapActiveTime = 1.1f;
    [Min(0f)] public float trapDamage = 7f;
    [Header("Presentation - sprites can be replaced without changing generators")]
    public Sprite mazeWall;
    public Color mazeTint = new Color(0.55f,0.75f,0.85f);
    public Color trapWarningColor = new Color(1f,0.75f,0.12f);
}
