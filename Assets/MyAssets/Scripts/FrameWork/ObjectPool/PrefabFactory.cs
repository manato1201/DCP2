using UnityEngine;

public interface IFactory
{
    APooledObject Create();              // 実体生成
    Transform PoolParent { get; }        // 生成物の親（Hierarchy整理）
}

// ふつうのPrefab用
[System.Serializable]
public sealed class PrefabFactory : IFactory
{
    [SerializeField] GameObject prefab;
    [SerializeField] Transform parent;

    public Transform PoolParent => parent;

    public APooledObject Create()
    {
        if (!prefab) return null;
        var go = Object.Instantiate(prefab, parent);
        if (!go.TryGetComponent(out APooledObject p))
        {
            Debug.LogError($"{prefab.name} に APooledObject 派生がありません");
            Object.Destroy(go);
            return null;
        }
        return p;
    }
}
