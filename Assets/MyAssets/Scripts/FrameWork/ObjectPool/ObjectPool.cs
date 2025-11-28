using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPool<T> where T : APooledObject
{
    readonly Stack<T> _pool = new();
    readonly IFactory _factory;
    readonly int _initial;
    readonly int _max;         // 0以下で無制限

    public int Count => _pool.Count;

    public ObjectPool(IFactory factory, int initial = 0, int max = 0)
    {
        _factory = factory;
        _initial = Mathf.Max(0, initial);
        _max = max;
    }

    // 旧IObjectPool互換
    public void PoolSetUp(uint index) => Prewarm();

    public void Prewarm()
    {
        for (int i = Count; i < _initial; i++)
        {
            var inst = Create();
            if (!inst) break;
            ReturnToPool(inst);  // 非アクティブで積む
        }
    }

    public T Get()
    {
        T item;
        // 在庫があれば使う
        if (_pool.Count > 0)
        {
            item = _pool.Pop();
            if (!item)                       // 破棄されていたら作り直す
                item = Create();
        }
        else
        {
            item = Create();
        }

        if (!item) return null;
        if (_factory?.PoolParent) item.transform.SetParent(_factory.PoolParent, false);

        item.gameObject.SetActive(true);
        item.Spawned();                      // ライフサイクル通知
        return item;
    }

    public APooledObject GetFromPool() => Get();

    public void ReturnToPool(APooledObject obj)
    {
        if (obj is not T t || !t) return;

        // 上限（溢れたら破棄）。0以下は無制限
        if (_max > 0 && _pool.Count >= _max)
        {
            Object.Destroy(t.gameObject);
            return;
        }

        t.gameObject.SetActive(false);
        t.transform.SetParent(_factory?.PoolParent, false);
        _pool.Push(t);
    }

    T Create()
    {
        var p = _factory?.Create() as T;
        if (!p) return null;
        p.SetReturn(ReturnToPool);           // ここで Release() の行き先を登録
        return p;
    }

    // 任意：全破棄
    public void Clear()
    {
        while (_pool.Count > 0)
        {
            var t = _pool.Pop();
            if (t) Object.Destroy(t.gameObject);
        }
    }
}
