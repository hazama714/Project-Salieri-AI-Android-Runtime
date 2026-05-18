/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Text;

namespace SalieriAI.CloudLLM
{
    public static class PromptTextUtility
    {
        public static void AppendStateLines(StringBuilder sb, global::SelfStateSummary state)
        {
            if (sb == null)
                return;

            if (state == null)
            {
                sb.AppendLine("状態: 不明");
                return;
            }

            sb.AppendLine("状態:");
            sb.AppendLine($"顔検出: {(state.faceDetected ? "あり" : "なし")}");
            sb.AppendLine($"最後に顔を見た時刻: {state.secondsSinceLastFace:F1}秒前");
            sb.AppendLine($"顔が見えている継続時間: {state.faceVisibleDuration:F1}秒");
            sb.AppendLine($"顔が見えていない継続時間: {state.noFaceDuration:F1}秒");
            sb.AppendLine($"最後の行動: {SafeText(state.lastAction)}");
            sb.AppendLine($"最後の行動から: {state.secondsSinceLastAction:F1}秒");
            sb.AppendLine($"最後の発話から: {state.secondsSinceLastSpeech:F1}秒");
            sb.AppendLine($"現在状態: {state.interactionState}");
        }

        public static string AppendCloudStateContext(string prompt, global::SelfStateSummary state)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(prompt))
                sb.AppendLine(prompt.Trim());

            sb.AppendLine();
            AppendStateLines(sb, state);

            return sb.ToString();
        }

        public static string SafeText(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ""
                : value.Trim();
        }
    }
}
