/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Text;
using UnityEngine;

namespace SalieriAI.LocalLLM
{
    public static class LocalActionPromptBuilder
    {
        public static string BuildLocalActionPrompt(global::SelfStateSummary summary)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("状態:");

            sb.AppendLine($"face={(summary.faceDetected ? 1 : 0)}");
            sb.AppendLine($"lastAction={Safe(summary.lastAction)}");
            sb.AppendLine($"lastActionSec={(int)summary.secondsSinceLastAction}");
            sb.AppendLine($"lastSpeechSec={(int)summary.secondsSinceLastSpeech}");
            sb.AppendLine($"state={Safe(summary.interactionState)}");

            sb.AppendLine($"camera={(summary.cameraAvailable ? 1 : 0)}");
            sb.AppendLine($"mic={(summary.microphoneAvailable ? 1 : 0)}");
            sb.AppendLine($"bluetooth={(summary.bluetoothReady ? 1 : 0)}");
            sb.AppendLine($"llm={(summary.llamaReady ? 1 : 0)}");
            sb.AppendLine($"voicevox={(summary.voicevoxReady ? 1 : 0)}");
            sb.AppendLine($"servo={(summary.canMoveServo ? 1 : 0)}");
            sb.AppendLine($"speak={(summary.canSpeak ? 1 : 0)}");

            sb.AppendLine(
                $"battery={(summary.batteryLevel < 0f ? -1 : Mathf.RoundToInt(summary.batteryLevel * 100f))}"
            );

            sb.AppendLine(
                $"charging={(summary.batteryStatus == BatteryStatus.Charging || summary.batteryStatus == BatteryStatus.Full ? 1 : 0)}"
            );

            sb.AppendLine($"temp={(int)summary.maxTemperatureCelsius}");

            sb.AppendLine();

            sb.AppendLine("行動:");
            sb.AppendLine("none");
            sb.AppendLine("lookAround");
            sb.AppendLine("returnCenter");
            sb.AppendLine("idleNod");
            sb.AppendLine("speakShort");

            return sb.ToString();
        }

        public static string BuildLocalActionInstructions()
        {
            return
                "action は speakShort を選んでください。\n" +
                "短く自然に話してください。\n" +
                "返答はJSONのみ。\n" +
                "形式は必ず {\"action\":\"speakShort\",\"speech\":\"...\"}";
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "none"
                : value;
        }
    }
}