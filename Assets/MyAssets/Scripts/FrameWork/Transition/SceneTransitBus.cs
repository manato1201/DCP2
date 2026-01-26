public static class SceneTransitBus
{
    public static SceneTransitData.Payload Payload;

    public static void Set(string chap, string key = null, bool isFade = false)
    {
        Payload = new SceneTransitData.Payload { chap = chap, key = key, isFade = isFade };
    }

    public static void Clear() => Payload = default;

    public static bool HasChap => !string.IsNullOrEmpty(Payload.chap);
}
