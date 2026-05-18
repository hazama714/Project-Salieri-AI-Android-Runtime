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
    public sealed class CloudActionProvider : MonoBehaviour, ILLMActionProvider
    {
        [Header("Cloud")]
        [SerializeField] private CloudLLMClient cloudClient;

        public async Task<LLMActionDecision> DecideActionAsync(SelfStateSummary summary)
        {
            if (cloudClient == null)
            {
                Debug.LogWarning("[CloudActionProvider] cloudClient is null");
                return LLMActionDecision.SafeDefault("cloud_client_missing");
            }

            if (summary == null)
            {
                Debug.LogWarning("[CloudActionProvider] summary is null");
                return LLMActionDecision.SafeDefault("summary_missing");
            }

            LLMActionDecision decision = await cloudClient.GenerateActionDecisionAsync(summary);

            if (decision == null)
                return LLMActionDecision.SafeDefault("cloud_action_null");

            decision.Normalize();
            return decision;
        }
    }
}