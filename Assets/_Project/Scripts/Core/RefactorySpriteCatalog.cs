using System;
using System.Collections.Generic;
using UnityEngine;

// Logical sprite keys are independent of texture locations. The Resources asset
// holds serialized references so these sprites are also included in player builds.
public sealed class RefactorySpriteCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry { public string key; public Sprite sprite; }
    public Entry[] entries = Array.Empty<Entry>();
    static RefactorySpriteCatalog instance;
    static Dictionary<string, Sprite> lookup;
    static bool attemptedLoad;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() { instance = null; lookup = null; attemptedLoad = false; }

    public static Sprite Load(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (!attemptedLoad)
        {
            attemptedLoad = true;
            instance = Resources.Load<RefactorySpriteCatalog>("RefactorySpriteCatalog");
            lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
            if (instance == null) Debug.LogError("RefactorySpriteCatalog asset is missing from Resources.");
            else foreach (var entry in instance.entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                if (!lookup.TryAdd(entry.key, entry.sprite)) Debug.LogError("Duplicate sprite catalog key: " + entry.key);
            }
        }
        return lookup.TryGetValue(key, out var sprite) ? sprite : null;
    }
}
