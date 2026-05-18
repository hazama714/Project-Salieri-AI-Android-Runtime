// v2025-09-02-ReleaseBus-01 (2025-09-02 22:40 JST)
// コメント: UI応答を全域に配信する単純な静的バス。既存Hubの有無に関わらずTTSへ通知を飛ばす。

using System;

public static class ResponseBus
{
    /// <summary>UIに応答テキストが表示されたら流されるイベント。</summary>
    public static event Action<string> OnResponse;

    /// <summary>発火（空文字は無視）</summary>
    public static void Raise(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        try
        {
            OnResponse?.Invoke(text);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[ResponseBus] 例外: {e.Message}");
        }
    }
}
