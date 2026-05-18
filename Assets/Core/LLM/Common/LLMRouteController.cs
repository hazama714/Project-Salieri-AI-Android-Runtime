/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using System.Threading.Tasks;
using UnityEngine;
using SalieriAI.Runtime;

namespace SalieriAI.Core.LLM.Common
{
    public enum LLMRouteMode
    {
        CloudOnly,
        LocalOnly,
        CloudPrimaryLocalFallback
    }

    public sealed class LLMRouteController : MonoBehaviour
    {
        [Header("Route")]
        [SerializeField] private LLMRouteMode routeMode = LLMRouteMode.CloudPrimaryLocalFallback;
        [SerializeField] private RuntimeConnectionSettings runtimeSettings;

        [Header("Providers")]
        [SerializeField] private MonoBehaviour cloudActionProviderBehaviour;
        [SerializeField] private MonoBehaviour localActionProviderBehaviour;
        [SerializeField] private MonoBehaviour cloudSpeechProviderBehaviour;
        [SerializeField] private MonoBehaviour localSpeechProviderBehaviour;

        private ILLMActionProvider CloudActionProvider =>
            cloudActionProviderBehaviour as ILLMActionProvider;

        private ILLMActionProvider LocalActionProvider =>
            localActionProviderBehaviour as ILLMActionProvider;

        private ILLMSpeechProvider CloudSpeechProvider =>
            cloudSpeechProviderBehaviour as ILLMSpeechProvider;

        private ILLMSpeechProvider LocalSpeechProvider =>
            localSpeechProviderBehaviour as ILLMSpeechProvider;

        private void Awake()
        {
            if (runtimeSettings == null)
            {
                runtimeSettings = FindObjectOfType<RuntimeConnectionSettings>();
            }
        }

        public async Task<LLMActionDecision> DecideActionAsync(SelfStateSummary summary)
        {
            if (IsLLMDisabled())
                return LLMActionDecision.SafeDefault("llm_disabled_by_runtime_settings");

            switch (GetCurrentRouteMode())
            {
                case LLMRouteMode.CloudOnly:
                    return await DecideActionCloudAsync(summary);

                case LLMRouteMode.LocalOnly:
                    return await DecideActionLocalAsync(summary);

                case LLMRouteMode.CloudPrimaryLocalFallback:
                    return await DecideActionCloudWithFallbackAsync(summary);

                default:
                    return LLMActionDecision.SafeDefault("unknown_route_mode");
            }
        }

        public async Task<LLMSpeechDecision> DecideSpeechAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            if (IsLLMDisabled())
                return LLMSpeechDecision.Empty("llm_disabled_by_runtime_settings");

            switch (GetCurrentRouteMode())
            {
                case LLMRouteMode.CloudOnly:
                    return await DecideSpeechCloudAsync(summary, actionDecision);

                case LLMRouteMode.LocalOnly:
                    return await DecideSpeechLocalAsync(summary, actionDecision);

                case LLMRouteMode.CloudPrimaryLocalFallback:
                    return await DecideSpeechCloudWithFallbackAsync(summary, actionDecision);

                default:
                    return LLMSpeechDecision.Empty("unknown_route_mode");
            }
        }

        private LLMRouteMode GetCurrentRouteMode()
        {
            if (runtimeSettings == null)
                return routeMode;

            bool cloudEnabled = runtimeSettings.useCloudLLM;
            bool localEnabled = runtimeSettings.useLocalLLM;

            if (!cloudEnabled && localEnabled)
                return LLMRouteMode.LocalOnly;

            if (cloudEnabled && !localEnabled)
                return LLMRouteMode.CloudOnly;

            if (!cloudEnabled && !localEnabled)
                return routeMode;

            switch (runtimeSettings.runtimeMode)
            {
                case RuntimeConnectionSettings.RuntimeMode.WifiCloudOnly:
                    return LLMRouteMode.CloudOnly;

                case RuntimeConnectionSettings.RuntimeMode.WifiOffLocalOnly:
                    return LLMRouteMode.LocalOnly;

                case RuntimeConnectionSettings.RuntimeMode.WifiCloudAndLocal:
                    if (runtimeSettings.ShouldUseCloudNow())
                        return LLMRouteMode.CloudPrimaryLocalFallback;

                    return LLMRouteMode.LocalOnly;

                default:
                    return routeMode;
            }
        }

        private bool IsLLMDisabled()
        {
            if (runtimeSettings == null)
                return false;

            return !runtimeSettings.useCloudLLM &&
                   !runtimeSettings.useLocalLLM;
        }

        private async Task<LLMActionDecision> DecideActionCloudAsync(SelfStateSummary summary)
        {
            if (CloudActionProvider == null)
                return LLMActionDecision.SafeDefault("cloud_action_provider_missing");

            LLMActionDecision decision =
                await CloudActionProvider.DecideActionAsync(summary);

            return NormalizeActionDecision(decision, "cloud_action_empty");
        }

        private async Task<LLMActionDecision> DecideActionLocalAsync(SelfStateSummary summary)
        {
            if (LocalActionProvider == null)
                return LLMActionDecision.SafeDefault("local_action_provider_missing");

            LLMActionDecision decision =
                await LocalActionProvider.DecideActionAsync(summary);

            return NormalizeActionDecision(decision, "local_action_empty");
        }

        private async Task<LLMActionDecision> DecideActionCloudWithFallbackAsync(SelfStateSummary summary)
        {
            try
            {
                LLMActionDecision cloudDecision =
                    await DecideActionCloudAsync(summary);

                if (cloudDecision != null && !cloudDecision.IsNone())
                    return cloudDecision;

                if (cloudDecision != null &&
                    cloudDecision.reason != "cloud_action_provider_missing")
                {
                    return cloudDecision;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LLMRouteController] Cloud action failed. fallback local. {ex.Message}"
                );
            }

            return await DecideActionLocalAsync(summary);
        }

        private async Task<LLMSpeechDecision> DecideSpeechCloudAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            if (CloudSpeechProvider == null)
                return LLMSpeechDecision.Empty("cloud_speech_provider_missing");

            LLMSpeechDecision decision =
                await CloudSpeechProvider.DecideSpeechAsync(summary, actionDecision);

            return NormalizeSpeechDecision(decision, "cloud_speech_empty");
        }

        private async Task<LLMSpeechDecision> DecideSpeechLocalAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            if (LocalSpeechProvider == null)
                return LLMSpeechDecision.Empty("local_speech_provider_missing");

            LLMSpeechDecision decision =
                await LocalSpeechProvider.DecideSpeechAsync(summary, actionDecision);

            return NormalizeSpeechDecision(decision, "local_speech_empty");
        }

        private async Task<LLMSpeechDecision> DecideSpeechCloudWithFallbackAsync(
            SelfStateSummary summary,
            LLMActionDecision actionDecision)
        {
            try
            {
                LLMSpeechDecision cloudDecision =
                    await DecideSpeechCloudAsync(summary, actionDecision);

                if (cloudDecision != null && cloudDecision.HasSpeech())
                    return cloudDecision;

                if (cloudDecision != null &&
                    cloudDecision.reason != "cloud_speech_provider_missing")
                {
                    return cloudDecision;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[LLMRouteController] Cloud speech failed. fallback local. {ex.Message}"
                );
            }

            return await DecideSpeechLocalAsync(summary, actionDecision);
        }

        private static LLMActionDecision NormalizeActionDecision(
            LLMActionDecision decision,
            string fallbackReason)
        {
            if (decision == null)
                return LLMActionDecision.SafeDefault(fallbackReason);

            decision.Normalize();
            return decision;
        }

        private static LLMSpeechDecision NormalizeSpeechDecision(
            LLMSpeechDecision decision,
            string fallbackReason)
        {
            if (decision == null)
                return LLMSpeechDecision.Empty(fallbackReason);

            decision.Normalize();
            return decision;
        }
    }
}