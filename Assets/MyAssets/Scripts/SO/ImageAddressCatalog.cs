using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(menuName = "Catalog/Image Address Catalog")]
public sealed class ImageAddressCatalog : ScriptableObject
{
    [System.Serializable] public struct Entry { public string Id; public AssetReferenceSprite Sprite; }
    public Entry[] Entries;

    public bool TryGetSpriteRef(string id, out AssetReferenceSprite aref)
    {
        for (int i = 0; i < (Entries?.Length ?? 0); i++)
            if (Entries[i].Id == id) { aref = Entries[i].Sprite; return aref != null; }
        aref = null; return false;
    }
}
