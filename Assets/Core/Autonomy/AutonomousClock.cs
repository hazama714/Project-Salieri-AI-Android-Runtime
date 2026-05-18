/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Threading.Tasks;
using SalieriAI.Core.LLM.Common;
using SalieriAI.Core.Limbo;
using UnityEngine;

namespace SalieriAI.Autonomy
{
    public sealed class AutonomousClock : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private RobotConditionCollector conditionCollector;
        [SerializeField] private LLMRouteController llmRouteController;
        [SerializeField] private AutonomousThinkService thinkService;
        [SerializeField] private LimboPermission limboPermission;

        [Header("Clock")]
        [SerializeField] private float thinkIntervalSeconds = 15f;
        [SerializeField] private bool runOnStart = true;

        [Header("Debug")]
        [SerializeField] private bool verboseLog = true;

        private bool isRunning;
        private bool isThinking;

        private void Start()
        {
            if (runOnStart)
                StartClock();
        }

        public void StartClock()
        {
            if (isRunning)
                return;

            isRunning = true;
            _ = ClockLoopAsync();
        }

        public void StopClock()
        {
            isRunning = false;
        }

        private async Task ClockLoopAsync()
        {
            if (verboseLog)
                Debug.Log("[AutonomousClock] started");

            while (isRunning)
            {
                try
                {
                    await TickAsync();
                }
                catch (System.Exception ex)
                {
                    Debug.LogException(ex);
                }

                await Task.Delay((int)(thinkIntervalSeconds * 1000f));
            }

            if (verboseLog)
                Debug.Log("[AutonomousClock] stopped");
        }

        private async Task TickAsync()
        {
            if (isThinking)
                return;

            if (!CanThinkNow())
                return;

            if (conditionCollector == null)
            {
                Debug.LogWarning("[AutonomousClock] conditionCollector is null");
                return;
            }

            if (llmRouteController == null)
            {
                Debug.LogWarning("[AutonomousClock] llmRouteController is null");
                return;
            }

            if (thinkService == null)
            {
                Debug.LogWarning("[AutonomousClock] thinkService is null");
                return;
            }

            SelfStateSummary summary = conditionCollector.BuildSummary();

            if (summary == null)
            {
                Debug.LogWarning("[AutonomousClock] summary is null");
                return;
            }

            isThinking = true;

            try
            {
                if (verboseLog)
                    Debug.Log("[AutonomousClock] think tick");

                LLMActionDecision actionDecision =
                    await llmRouteController.DecideActionAsync(summary);

                await thinkService.ExecuteAsync(summary, actionDecision);
            }
            finally
            {
                isThinking = false;
            }
        }

        private bool CanThinkNow()
        {
            if (limboPermission == null)
                return true;

            if (limboPermission.IsEmergencyMode)
                return false;

            if (!limboPermission.CanThink)
                return false;

            if (!limboPermission.CanStartAction)
                return false;

            return true;
        }
    }
}