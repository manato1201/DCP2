using UnityEngine;

public class Shooter : MonoBehaviour
{
    [SerializeField] PrefabFactory pieceFactory; // ← inspectorで prefab と parent を割当
    ObjectPool<piece> pool;

    void Awake()
    {
        pool = new ObjectPool<piece>(pieceFactory, initial:3, max:12); // 初期在庫/上限
        pool.Prewarm(); // 事前生成して非アクティブで積む
    }

    void Shoot(Transform muzzle)
    {
        var b = pool.Get();                    // 取り出す
        if (!b) return;
        b.transform.SetPositionAndRotation(muzzle.position, muzzle.rotation);
        // あとは Bullet 側の Update/衝突で Release() されて戻る
    }
}
