using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum SceneId { Title, Select,BookUI, Story, Mission, Puzzle, Result }

[CreateAssetMenu(menuName = "Config/SceneAddressCatalog")]
public sealed class SceneAddressCatalog : ScriptableObject
{
    [System.Serializable]
    public struct Entry { public SceneId id; public string address; }

    public Entry[] entries;

    public string Get(SceneId id)
    {
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].id == id) return entries[i].address;
        Debug.LogError($"[Catalog] address not found for {id}");
        return null;
    }
}
