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

namespace SalieriAI.CloudLLM
{
    public sealed class CloudSpeechProvider : MonoBehaviour, ILLMSpeechProvider
    {
        [Header("Cloud")]
        [SerializeField] private CloudLLMClient cloudClient;

        public async Task<LLMSpeechDecision> DecideSpeechAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            if (cloudClient == null)
            {
                Debug.LogWarning("[CloudSpeechProvider] cloudClient is null");
                return LLMSpeechDecision.Empty("cloud_client_missing");
            }

            if (summary == null)
            {
                Debug.LogWarning("[CloudSpeechProvider] summary is null");
                return LLMSpeechDecision.Empty("summary_missing");
            }

            if (actionDecision == null)
                actionDecision = LLMActionDecision.SafeDefault("action_missing");

            actionDecision.Normalize();

            string speech = await cloudClient.GenerateSpeechForActionAsync(summary, actionDecision);

            if (string.IsNullOrWhiteSpace(speech))
                return LLMSpeechDecision.Empty("cloud_speech_empty");

            return new LLMSpeechDecision
            {
                speech = speech.Trim(),
                reason = actionDecision.reason,
                confidence = actionDecision.confidence
            };
        }
    }
}