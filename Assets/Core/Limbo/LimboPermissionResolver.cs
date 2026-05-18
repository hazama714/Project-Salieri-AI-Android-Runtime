/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 */

using UnityEngine;
using SalieriAI.Core.State;

namespace SalieriAI.Core.Limbo
{
    public sealed class LimboPermissionResolver : MonoBehaviour
    {
        [SerializeField] private InteractionStateController stateController;
        [SerializeField] private LimboPermission limboPermission;

        [Header("Startup Safety")]
        [SerializeField] private float idleTrackFaceDelaySeconds = 1.5f;

        private InteractionState lastState;
        private float idleEnteredTime = -1f;
        private bool idleTrackFaceReleased;

        private void Start()
        {
            Apply();
        }

        private void Update()
        {
            if (stateController == null)
                return;

            if (lastState == stateController.CurrentState)
            {
                ApplyIdleDelayOverride();
                return;
            }

            Apply();
        }

        private void Apply()
        {
            if (stateController == null || limboPermission == null)
                return;

            lastState = stateController.CurrentState;

            if (lastState == InteractionState.Idle)
            {
                idleEnteredTime = Time.time;
                idleTrackFaceReleased = false;
            }
            else
            {
                idleEnteredTime = -1f;
                idleTrackFaceReleased = false;
            }

            limboPermission.LockAll();

            switch (lastState)
            {
                case InteractionState.Booting:
                    limboPermission.AllowServo();
                    break;

                case InteractionState.Idle:
                    limboPermission.EnterRuntime();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Tracking:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowSpeak();
                    limboPermission.AllowThinking();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.TemporaryLost:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowSearch();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.FullyLost:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowSearch();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Searching:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowSearch();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Thinking:
                    limboPermission.AllowServo();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Speaking:
                    limboPermission.AllowServo();
                    limboPermission.AllowSpeak();
                    limboPermission.AllowInterrupt();

                    // î≠òbíÜÇÕäÁí«ê’Çé~ÇﬂÇÈ
                    limboPermission.DenyFaceTracking();
                    break;

                case InteractionState.Listening:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Acting:
                    limboPermission.AllowServo();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Recovering:
                    limboPermission.AllowFaceTracking();
                    limboPermission.AllowServo();
                    limboPermission.AllowSpeak();
                    limboPermission.AllowInterrupt();
                    break;

                case InteractionState.Emergency:
                    limboPermission.EnterEmergencyMode();
                    break;
            }

            ApplyIdleDelayOverride();

            Debug.Log($"[LimboPermissionResolver] Applied: {lastState}");
        }

        private void ApplyIdleDelayOverride()
        {
            if (limboPermission == null)
                return;

            if (lastState != InteractionState.Idle)
                return;

            if (idleEnteredTime < 0f)
                return;

            float elapsed = Time.time - idleEnteredTime;

            if (elapsed < idleTrackFaceDelaySeconds)
            {
                limboPermission.DenyFaceTracking();

                Debug.Log(
                    $"[LimboPermissionResolver] Idle delay: " +
                    $"CanTrackFace=false elapsed:{elapsed:F2}"
                );

                return;
            }

            if (!idleTrackFaceReleased)
            {
                limboPermission.AllowFaceTracking();
                idleTrackFaceReleased = true;

                Debug.Log("[LimboPermissionResolver] Idle delay finished: CanTrackFace=true");
            }
        }
    }
}