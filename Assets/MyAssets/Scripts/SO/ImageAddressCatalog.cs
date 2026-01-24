using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AddressCatalog/Image Address Catalog")]
public sealed class ImageAddressCatalog : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string Id;     // 例: "player1", "Bg_SchoolYard"
        public Sprite Sprite; // 直接参照
    }

    [SerializeField] private List<Entry> entries = new();

    Dictionary<string, Sprite> _map;

    void OnEnable() => BuildMap();

#if UNITY_EDITOR
    // インスペクタで編集したときも再構築
    void OnValidate() => BuildMap();
#endif

    void BuildMap()
    {
        _map = new Dictionary<string, Sprite>(entries.Count);
        foreach (var e in entries)
        {
            if (string.IsNullOrWhiteSpace(e.Id) || e.Sprite == null) continue;
            _map[e.Id] = e.Sprite; // 同一Idは後勝ち
        }
    }

    public bool TryGetSprite(string id, out Sprite sprite)
    {
        sprite = null; // ← これで全経路代入
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (_map == null) BuildMap();
        return _map != null && _map.TryGetValue(id, out sprite);
    }
}
