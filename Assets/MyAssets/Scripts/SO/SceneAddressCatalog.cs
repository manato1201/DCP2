using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum SceneId { Title, Select,BookUI, Story, Mission, Puzzle, Result }

[CreateAssetMenu(menuName = "Config/SceneAddressCatalog")]
public sealed class SceneAddressCatalog : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public SceneId id;
        public AssetReference scene;  // ← Addressable化した .unity をInspectorで割り当て
    }
    public Entry[] entries;

    public AssetReference Get(SceneId id)
    {
        foreach (var e in entries) if (e.id == id) return e.scene;
        return null;
    }
}
