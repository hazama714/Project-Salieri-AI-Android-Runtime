/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Threading.Tasks;
using SalieriAI.AndroidLLM;
using SalieriAI.Core.LLM;
using SalieriAI.Core.LLM.Common;
using UnityEngine;

namespace SalieriAI.LocalLLM
{
    public sealed class LocalActionProvider : MonoBehaviour, ILLMActionProvider
    {
        [Header("Local")]
        [SerializeField] private LocalLLMRuntimeHost localHost;
        [SerializeField] private LLMGenerationProfile actionProfile;

        [Header("Debug")]
        [SerializeField] private bool logPrompt = false;
        [SerializeField] private bool logRawResponse = true;

        public async Task<LLMActionDecision> DecideActionAsync(SelfStateSummary summary)
        {
            if (localHost == null)
            {
                Debug.LogWarning("[LocalActionProvider] localHost is null");
                return LLMActionDecision.SafeDefault("local_host_missing");
            }

            if (summary == null)
            {
                Debug.LogWarning("[LocalActionProvider] summary is null");
                return LLMActionDecision.SafeDefault("summary_missing");
            }

            try
            {
                string prompt =
                    LocalActionPromptBuilder.BuildLocalActionInstructions()
                    + "\n\n"
                    + LocalActionPromptBuilder.BuildLocalActionPrompt(summary);

                if (logPrompt)
                    Debug.Log($"[LocalActionProvider] PROMPT:\n{prompt}");

                string raw = await localHost.GenerateSafeAsync(
                    prompt,
                    actionProfile,
                    "local_action"
                );

                if (logRawResponse)
                    Debug.Log($"[LocalActionProvider] RAW: {raw}");

                if (!LLMActionResponseParser.TryParse(raw, out LLMActionDecision decision))
                {
                    Debug.LogWarning("[LocalActionProvider] parse failed");
                    return LLMActionDecision.SafeDefault("local_action_parse_failed");
                }

                if (decision == null)
                    return LLMActionDecision.SafeDefault("local_action_null");

                decision.Normalize();
                return decision;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LocalActionProvider] failed: {ex.GetType().Name}: {ex.Message}");
                return LLMActionDecision.SafeDefault("local_action_exception");
            }
        }
    }
}