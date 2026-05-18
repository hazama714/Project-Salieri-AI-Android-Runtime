/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Threading.Tasks;
using SalieriAI.Core.LLM.Common;
using UnityEngine;

namespace SalieriAI.LocalLLM
{
    public sealed class LocalSpeechProvider : MonoBehaviour, ILLMSpeechProvider
    {
        [SerializeField] private string defaultLookAroundSpeech = "è≠ÇµíTÇµÇƒÇ¢Ç‹Ç∑ÅB";
        [SerializeField] private string defaultReturnCenterSpeech = "ê≥ñ Ç…ñﬂÇËÇ‹Ç∑ÅB";
        [SerializeField] private string defaultIdleSpeech = "è≠Çµë“Ç¡ÇƒÇ¢Ç‹Ç∑ÅB";

        public Task<LLMSpeechDecision> DecideSpeechAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            if (actionDecision == null)
                return Task.FromResult(LLMSpeechDecision.Empty("action_missing"));

            actionDecision.Normalize();

            if (!string.IsNullOrWhiteSpace(actionDecision.speech))
            {
                return Task.FromResult(new LLMSpeechDecision
                {
                    speech = actionDecision.speech,
                    reason = actionDecision.reason,
                    confidence = actionDecision.confidence
                });
            }

            string speech = GetFallbackSpeech(actionDecision.action);

            if (string.IsNullOrWhiteSpace(speech))
                return Task.FromResult(LLMSpeechDecision.Empty("local_speech_none"));

            return Task.FromResult(new LLMSpeechDecision
            {
                speech = speech,
                reason = "local_fallback_speech",
                confidence = 0.4f
            });
        }

        private string GetFallbackSpeech(string action)
        {
            switch (action)
            {
                case "lookAround":
                    return defaultLookAroundSpeech;

                case "returnCenter":
                    return defaultReturnCenterSpeech;

                case "speakShort":
                    return defaultIdleSpeech;

                default:
                    return "";
            }
        }
    }
}