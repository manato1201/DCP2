using System;
using UnityEngine;

// プール対象の共通基底。二度戻し防止・ライフサイクル通知付き
public abstract class APooledObject : MonoBehaviour
{
    Action<APooledObject> _return;   // 登録は1回だけ
    bool _inPool;                    // 二度戻し防止

    // プールに返す（使う側はこれだけ覚えればいい）
    public void Release()
    {
        if (_inPool) return;         // すでに在庫に戻っている
        _inPool = true;
        OnDespawn();                 // 消滅時コールバック
        _return?.Invoke(this);
    }

    // プールから取り出された直後に呼ばれる
    internal void Spawned()
    {
        _inPool = false;
        OnSpawn();
    }

    internal void SetReturn(Action<APooledObject> action) => _return = action;

    // 任意の挙動（必要なら派生でオーバーライド）
    protected virtual void OnSpawn() { }
    protected virtual void OnDespawn() { }
}
