using UnityEngine;

public sealed class piece : APooledObject
{
    //例

    // [SerializeField] float speed = 12f;
    // [SerializeField] float lifeTime = 2f;
    // float t;
    //
    // protected override void OnSpawn() { t = 0f; }   // 取り出された直後
    // protected override void OnDespawn() { /* 状態リセットがあれば */ }
    //
    // void Update()
    // {
    //     transform.position += transform.forward * speed * Time.deltaTime;
    //     t += Time.deltaTime;
    //     if (t >= lifeTime) Release();               // 使い終わったら Release()
    // }
    //
    // void OnTriggerEnter(Collider _) => Release();   // 命中で返却
}
