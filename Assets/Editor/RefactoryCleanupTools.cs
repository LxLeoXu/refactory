using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only validation. Scene previews are closed without saving.
public static class RefactoryCleanupTools
{
    [MenuItem("Tools/REFACTORY/Cleanup/Validate references")]
    public static void Audit()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode before validation.");
        var lines = new List<string>();
        int prefabCount=0,sceneCount=0;
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && File.Exists(p)))
        {
            string ext=Path.GetExtension(path);
            if (new[]{".unity",".prefab",".asset",".mat",".anim",".controller",".spriteatlas"}.Contains(ext))
                foreach (Match match in Regex.Matches(File.ReadAllText(path), @"guid: ([0-9a-f]{32})"))
                {
                    string guid=match.Groups[1].Value;
                    if (!guid.StartsWith("0000000000000000") && string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid))) lines.Add("Unresolved GUID | " + path + " | " + guid);
                }
            if (ext==".prefab")
            {
                var root=PrefabUtility.LoadPrefabContents(path);
                try { Inspect(root,path,lines); prefabCount++; }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            if (ext==".unity")
            {
                var scene=EditorSceneManager.OpenPreviewScene(path);
                try { foreach (var root in scene.GetRootGameObjects()) Inspect(root,path,lines); sceneCount++; }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
        }
        var catalog = Resources.Load<RefactorySpriteCatalog>("RefactorySpriteCatalog");
        if (catalog == null) lines.Add("Sprite catalog missing");
        else
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach(var entry in catalog.entries)
            {
                if(!keys.Add(entry.key)) lines.Add("Duplicate catalog key | " + entry.key);
                if(entry.sprite == null || RefactorySpriteCatalog.Load(entry.key) != entry.sprite) lines.Add("Catalog sprite failed | " + entry.key);
                else if(!AssetDatabase.GetAssetPath(entry.sprite).StartsWith("Assets/_Project/Art/Textures/")) lines.Add("Sprite outside central folder | " + entry.key);
            }
        }
        foreach (string key in new[]{"items","recipes"}) if (Resources.Load<TextAsset>(key)==null) lines.Add("Resource text failed | " + key);
        foreach (string name in new[]{"MainMenu","LevelScene"})
            if (!EditorBuildSettings.scenes.Any(s=>s.enabled && Path.GetFileNameWithoutExtension(s.path)==name && File.Exists(s.path))) lines.Add("Build scene failed | " + name);
        lines=lines.Distinct().ToList();
        lines.Insert(0,"Validated " + prefabCount + " prefabs and " + sceneCount + " scenes. Compilation succeeded for this editor assembly.");
        File.WriteAllLines("PROJECT_CLEANUP_VALIDATION.txt",lines);
        Debug.Log("REFACTORY validation complete: " + (lines.Count-1) + " findings.");
    }
    static void Inspect(GameObject root,string path,List<string> lines)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            int missing=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            if (missing>0) lines.Add("Missing scripts | " + path + " | " + t.name + " | " + missing);
            foreach (var c in t.GetComponents<Component>())
            {
                if (c==null) continue;
                var so=new SerializedObject(c); var prop=so.GetIterator();
                while(prop.Next(true))
                    if(prop.propertyType==SerializedPropertyType.ObjectReference && prop.objectReferenceValue==null && prop.objectReferenceInstanceIDValue!=0)
                        lines.Add("Missing object | " + path + " | " + t.name + " | " + c.GetType().Name + "." + prop.propertyPath);
            }
        }
    }
}
