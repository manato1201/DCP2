using UnityEngine;

[CreateAssetMenu(menuName = "Config/SceneTransitData")]
public sealed class SceneTransitData : ScriptableObject
{
    [System.Serializable]
    public struct Payload
    {
        public string chap;
        public string key;
        public int intVal;
        public float floatVal;
        public string strVal;
        public bool isFade;
    }
    public Payload payload; // シンプル例。必要に応じて拡張
}
