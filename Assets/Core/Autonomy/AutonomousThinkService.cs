/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Threading.Tasks;
using UnityEngine;

namespace SalieriAI.Autonomy
{
    public sealed class AutonomousThinkService : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private BodyActionExecutor bodyActionExecutor;
        [SerializeField] private AutonomousSpeechService speechService;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = true;

        public async Task ExecuteAsync(
            SelfStateSummary summary,
            LLMActionDecision decision)
        {
            if (decision == null)
            {
                Debug.LogWarning("[AutonomousThinkService] decision is null");
                return;
            }

            decision.Normalize();

            if (verboseLog)
            {
                Debug.Log(
                    $"[AutonomousThinkService] action={decision.action} " +
                    $"reason={decision.reason}"
                );
            }

            if (bodyActionExecutor == null)
            {
                Debug.LogWarning("[AutonomousThinkService] bodyActionExecutor is null");
                return;
            }

            bodyActionExecutor.Execute(decision);

            if (speechService != null)
                await speechService.SpeakForActionAsync(summary, decision);
        }
    }
}