/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace SalieriAI.Core.LLM.Common
{
    /// <summary>
    /// Parses LLM action responses.
    ///
    /// Responsibility:
    /// - Extract action JSON from raw LLM text.
    /// - Remove extra assistant/template markers.
    /// - Normalize action names.
    /// - Salvage action when JSON is partially broken.
    ///
    /// This class does not call LLM and does not execute actions.
    /// </summary>
    public static class LLMActionResponseParser
    {
        [Serializable]
        private sealed class ActionDto
        {
            public string action;
            public string reason;
            public string speech;
            public float confidence;
        }

        public static bool TryParse(string raw, out LLMActionDecision decision)
        {
            decision = null;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string cleaned = CleanRaw(raw);

            if (TryParseJson(cleaned, out decision))
                return true;

            if (TrySalvageAction(cleaned, out decision))
                return true;

            return false;
        }

        private static bool TryParseJson(string cleaned, out LLMActionDecision decision)
        {
            decision = null;

            string json = ExtractJsonObject(cleaned);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                ActionDto dto = JsonUtility.FromJson<ActionDto>(json);

                if (dto == null || string.IsNullOrWhiteSpace(dto.action))
                    return false;

                string action = NormalizeAction(dto.action);

                decision = new LLMActionDecision
                {
                    action = action,
                    reason = string.IsNullOrWhiteSpace(dto.reason)
                        ? "llm_action_json"
                        : dto.reason.Trim(),
                    speech = dto.speech ?? string.Empty,
                    confidence = dto.confidence > 0f ? dto.confidence : 0.5f
                };

                decision.Normalize();
                return IsAllowedAction(decision.action);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMActionResponseParser] Json parse failed: {ex.GetType().Name} {ex.Message}");
                return false;
            }
        }

        private static bool TrySalvageAction(string cleaned, out LLMActionDecision decision)
        {
            decision = null;

            string action = ExtractActionValue(cleaned);
            if (string.IsNullOrWhiteSpace(action))
                return false;

            action = NormalizeAction(action);

            if (!IsAllowedAction(action))
                return false;

            decision = new LLMActionDecision
            {
                action = action,
                reason = "llm_action_salvaged",
                speech = string.Empty,
                confidence = 0.4f
            };

            decision.Normalize();
            return true;
        }

        private static string CleanRaw(string raw)
        {
            string text = raw.Trim();

            text = text.Replace("```json", string.Empty)
                       .Replace("```", string.Empty)
                       .Replace("<|assistant|>", string.Empty)
                       .Replace("<|ƒAƒVƒXƒ^ƒ“ƒg|>", string.Empty)
                       .Replace("<bAssistantb>", string.Empty)
                       .Replace("<bassistantb>", string.Empty)
                       .Replace("<|end|>", string.Empty)
                       .Trim();

            return text;
        }

        private static string ExtractJsonObject(string text)
        {
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');

            if (start < 0 || end <= start)
                return string.Empty;

            return text.Substring(start, end - start + 1).Trim();
        }

        private static string ExtractActionValue(string text)
        {
            Match match = Regex.Match(
                text,
                "\"action\"\\s*:\\s*\"(?<action>[^\"]+)\"",
                RegexOptions.IgnoreCase
            );

            if (!match.Success)
                return string.Empty;

            return match.Groups["action"].Value;
        }

        private static string NormalizeAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return "none";

            string a = action.Trim();

            switch (a)
            {
                case "none":
                case "lookAround":
                case "returnCenter":
                case "speakShort":
                    return a;

                case "lookaround":
                case "look_around":
                case "look-around":
                    return "lookAround";

                case "returncenter":
                case "return_center":
                case "return-center":
                case "center":
                    return "returnCenter";

                case "speak":
                case "speech":
                case "speak_short":
                case "speak-short":
                    return "speakShort";

                default:
                    return "none";
            }
        }

        private static bool IsAllowedAction(string action)
        {
            return action == "none"
                || action == "lookAround"
                || action == "returnCenter"
                || action == "speakShort";
        }
    }
}