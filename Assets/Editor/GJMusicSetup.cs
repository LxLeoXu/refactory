#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GJMusicSetup
{
    private const string Folder = "Assets/GameJamMusic/Tracks/";

    [MenuItem("Tools/GameJam/Set Up Game Music")]
    public static void Setup()
    {
        GameObject obj = GameObject.Find("GameJamMusicManager");
        if (obj == null)
        {
            obj = new GameObject("GameJamMusicManager");
            Undo.RegisterCreatedObjectUndo(obj, "Create GameJam music manager");
        }

        GJGameMusic music = obj.GetComponent<GJGameMusic>();
        if (music == null) music = Undo.AddComponent<GJGameMusic>(obj);

        Undo.RecordObject(music, "Assign music tracks");
        music.baseMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "Base_Factory.ogg");
        music.dungeonMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "Dungeon_Exploration.ogg");
        music.bossMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "ForgeHeart_Boss.ogg");
        EditorUtility.SetDirty(music);
        if (obj.scene.IsValid()) EditorSceneManager.MarkSceneDirty(obj.scene);
        Selection.activeGameObject = obj;

        if (music.baseMusic == null || music.dungeonMusic == null || music.bossMusic == null)
            Debug.LogWarning("GameJam Music: Niektory OGG subor chyba v " + Folder + ". Skontroluj import.");
        else
            Debug.Log("GameJam Music: 3 hudobne stopy priradene. MusicManager pripraveny (Base/Dungeon/Boss).");
    }
}
#endif
