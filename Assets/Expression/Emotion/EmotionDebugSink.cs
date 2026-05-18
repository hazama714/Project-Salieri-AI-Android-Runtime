// Version: Salieri.EmotionDebugSink v1.3.0-LabelOnly
// Timestamp (JST): 2025-10-17 23:59:59
// Comment:
// - 絵文字機能を全面撤去：DisplayMode/Emojiマップ/ComposeDisplayの絵文字結合を削除
// - UIは“ラベルのみ”表示（例: "Joy", "Neutral"）
// - 既存API不変更：OnEmotionReady(...), EmotionReady イベントは維持
// - 他の機能（ログ、再発火、TitleCase、Neutralフォールバック）は維持

using UnityEngine;
using TMPro;
using System;

namespace Salieri.Emotion
{
    public sealed class EmotionDebugSink : MonoBehaviour
    {
        [Header("UI Target (任意)")]
        [Tooltip("感情ラベルを表示する TextMeshProUGUI。未設定ならUI更新はスキップ")]
        public TMP_Text uiText;

        // 既存：確定した感情を外部へ通知（UI解放/DBコミット等）
        public event Action<EmotionResult> EmotionReady;

        [Header("表示設定")]
        [Tooltip("ラベルの先頭を大文字に整形する")]
        public bool titleCase = false;

        [Tooltip("ラベルが空/不明のとき Neutral を表示する")]
        public bool fallbackToNeutral = true;

        [Header("Debug Log")]
        public bool debugLog = true;

        // 既存API：変更しない
        public void OnEmotionReady(EmotionResult result)
        {
            // 1) ログ（従来通り）
            if (result == null)
            {
                if (debugLog) Debug.LogWarning("[EmotionDebug] Emotion: (null)");
                // UIはフォールバック表示
                if (uiText != null)
                {
                    var disp = BuildLabelOnly(null);
                    uiText.text = disp;
                    if (debugLog) Debug.Log($"[EmotionDebug/UI] set -> obj={uiText.name}, active={uiText.gameObject.activeInHierarchy}, text=\"{uiText.text}\"");
                }
                EmotionReady?.Invoke(result);
                return;
            }

            if (debugLog)
            {
                Debug.Log($"[EmotionDebug] Emotion: {result.primary} (intensity={result.intensity:0.00})");
                if (result.blendshapes != null)
                {
                    for (int i = 0; i < result.blendshapes.Length; i++)
                    {
                        var b = result.blendshapes[i];
                        Debug.Log($"  - {b.key}: {b.weight:0.00}");
                    }
                }
            }

            // 2) UI更新：ラベルのみ
            if (uiText != null)
            {
                var label = SafeLabel(result);
                var disp = BuildLabelOnly(label);
                uiText.text = disp;

                if (debugLog)
                    Debug.Log($"[EmotionDebug/UI] set -> obj={uiText.name}, active={uiText.gameObject.activeInHierarchy}, text=\"{uiText.text}\"");
            }
            else
            {
                if (debugLog) Debug.LogWarning("[EmotionDebugSink] uiText is null – skip UI update.");
            }

            // 3) 外部通知（既存のまま）
            EmotionReady?.Invoke(result);
        }

        // ===== 表示（ラベルのみ） =====
        private string BuildLabelOnly(string labelRaw)
        {
            var label = NormalizeCase(labelRaw);
            if (fallbackToNeutral && string.IsNullOrWhiteSpace(label)) label = "Neutral";
            return label ?? string.Empty;
        }

        private string NormalizeCase(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            if (!titleCase) return s;
            return char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1) : "");
        }

        private static string SafeLabel(EmotionResult r)
        {
            if (r == null) return null;
            try
            {
                if (!string.IsNullOrWhiteSpace(r.primary)) return r.primary;
            }
            catch { /* no-throw */ }
            var s = r.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
    }
}
