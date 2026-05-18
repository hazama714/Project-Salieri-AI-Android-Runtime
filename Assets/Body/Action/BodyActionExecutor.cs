/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections;
using SalieriAI.Core.Action;
using SalieriAI.Core.Affordance;
using SalieriAI.Core.Limbo;
using SalieriAI.Core.State;
using SalieriAI.Expression.Voice;
using UnityEngine;

public sealed class BodyActionExecutor : MonoBehaviour
{
    private const string ActionNone = "none";
    private const string ActionLookAround = "lookAround";
    private const string ActionReturnCenter = "returnCenter";
    private const string ActionIdleNod = "idleNod";
    private const string ActionSpeakShort = "speakShort";

    [Header("References")]
    [SerializeField] private NeckController neckController;
    [SerializeField] private VoiceController voiceController;

    [Header("State / Limbo")]
    [SerializeField] private LimboPermission limboPermission;
    [SerializeField] private InteractionStateController stateController;
    [SerializeField] private RobotConditionCollector robotConditionCollector;

    [Header("Override")]
    [SerializeField] private MonoBehaviour overrideComponent;
    [SerializeField] private float actionOverrideSeconds = 2.0f;

    [Header("Voice")]
    [SerializeField] private string defaultSpeakerName = "ñªñ¬Ç–Ç‹ÇË";
    [SerializeField] private string defaultShortSpeech = "è≠ÇµçlÇ¶ÇƒÇ¢Ç‹ÇµÇΩÅB";

    [Header("Neck Motion Debug Values")]
    [SerializeField] private float lookAroundYaw = 25f;
    [SerializeField] private float nodPitch = -10f;

    private Coroutine resumeFaceTrackingCoroutine;

    private float lastOverrideYaw = 90f;
    private float lastOverridePitch = 0f;

    private InteractionState stateBeforeAction = InteractionState.Idle;

    private void Awake()
    {
        if (limboPermission == null)
            limboPermission = FindObjectOfType<LimboPermission>();

        if (stateController == null)
            stateController = FindObjectOfType<InteractionStateController>();

        if (robotConditionCollector == null)
            robotConditionCollector = FindObjectOfType<RobotConditionCollector>();
    }

    public void Execute(LLMActionDecision decision)
    {
        if (decision == null)
        {
            Debug.LogWarning("[BodyActionExecutor] decision is null");
            return;
        }

        decision.Normalize();

        string action = NormalizeActionName(decision.action);

        Debug.Log($"[BodyActionExecutor] action={action} reason={decision.reason}");

        if (action == ActionNone)
            return;

        if (!CanStartActionNow(action))
            return;

        robotConditionCollector?.NotifyAction(action);
        Debug.Log($"[BodyActionExecutor] NotifyAction action={action}");

        EnterActingState();

        try
        {
            switch (action)
            {
                case ActionLookAround:
                    LookAround();
                    break;

                case ActionReturnCenter:
                    ReturnCenter();
                    break;

                case ActionIdleNod:
                    IdleNod();
                    break;

                case ActionSpeakShort:
                    Debug.Log("[BodyActionExecutor] speakShort selected");
                    ExitActingState();
                    break;

                default:
                    Debug.LogWarning($"[BodyActionExecutor] unknown LLM action: {action}");
                    ExitActingState();
                    break;
            }
        }
        catch
        {
            ExitActingState();
            throw;
        }
    }

    public ActionResult Execute(AffordanceCandidate candidate)
    {
        if (candidate == null)
            return ActionResult.Failed("null", "AffordanceCandidate is null.");

        Debug.Log(
            $"[BodyActionExecutor] affordance={candidate.Type} " +
            $"actionId={candidate.ActionId} reason={candidate.Reason}"
        );

        string action = NormalizeActionName(candidate.ActionId);

        if (action == ActionNone)
            return ActionResult.Ok(ActionNone, "No action executed.");

        if (!CanStartActionNow(action))
            return ActionResult.Failed(action, "Blocked by Limbo.");

        Execute(new LLMActionDecision
        {
            action = action,
            speech = "",
            reason = candidate.Reason,
            confidence = 1f
        });

        return ActionResult.Ok(action, "Action executed.");
    }

    private static string NormalizeActionName(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            return ActionNone;

        switch (action)
        {
            case "none":
                return ActionNone;

            case "lookAround":
            case "search_front":
            case "search":
                return ActionLookAround;

            case "returnCenter":
            case "look_front":
            case "look_at_target":
            case "hold_position":
                return ActionReturnCenter;

            case "idleNod":
            case "nod":
                return ActionIdleNod;

            case "speakShort":
            case "speak_short":
            case "speak":
                return ActionSpeakShort;

            default:
                return action;
        }
    }

    private void LookAround()
    {
        if (!CanMoveServoNow(ActionLookAround))
            return;

        Debug.Log($"[BodyActionExecutor] LookAround requested yaw={lookAroundYaw}");

        lastOverrideYaw = 90f + lookAroundYaw;
        lastOverridePitch = 0f;

        PauseFaceTrackingTemporarily();

        if (neckController == null)
        {
            Debug.LogWarning("[BodyActionExecutor] neckController is null");
            ExitActingState();
            return;
        }

        neckController.LookAt(lookAroundYaw, 0f);
    }

    private void ReturnCenter()
    {
        if (!CanMoveServoNow(ActionReturnCenter))
            return;

        Debug.Log("[BodyActionExecutor] ReturnCenter requested");

        lastOverrideYaw = 90f;
        lastOverridePitch = 0f;

        PauseFaceTrackingTemporarily();

        if (neckController == null)
        {
            Debug.LogWarning("[BodyActionExecutor] neckController is null");
            ExitActingState();
            return;
        }

        neckController.ReturnCenter();
    }

    private void IdleNod()
    {
        if (!CanMoveServoNow(ActionIdleNod))
            return;

        Debug.Log($"[BodyActionExecutor] IdleNod requested pitch={nodPitch}");

        lastOverrideYaw = 90f;
        lastOverridePitch = nodPitch;

        PauseFaceTrackingTemporarily();

        if (neckController == null)
        {
            Debug.LogWarning("[BodyActionExecutor] neckController is null");
            ExitActingState();
            return;
        }

        neckController.LookAt(0f, nodPitch);
    }

    private bool CanStartActionNow(string actionName)
    {
        if (limboPermission == null)
            return true;

        if (limboPermission.IsEmergencyMode)
        {
            Debug.Log($"[BodyActionExecutor] Blocked by Limbo Emergency action={actionName}");
            return false;
        }

        if (!limboPermission.CanStartAction)
        {
            Debug.Log($"[BodyActionExecutor] Blocked by Limbo CanStartAction=false action={actionName}");
            return false;
        }

        return true;
    }

    private bool CanMoveServoNow(string actionName)
    {
        if (limboPermission == null)
            return true;

        if (limboPermission.IsEmergencyMode)
        {
            Debug.Log($"[BodyActionExecutor] Servo blocked by Emergency action={actionName}");
            ExitActingState();
            return false;
        }

        if (!limboPermission.CanMoveServo)
        {
            Debug.Log($"[BodyActionExecutor] Servo blocked by Limbo action={actionName}");
            ExitActingState();
            return false;
        }

        return true;
    }

    private bool CanSpeakNow()
    {
        if (limboPermission == null)
            return true;

        if (limboPermission.IsEmergencyMode)
        {
            Debug.Log("[BodyActionExecutor] Speak blocked by Emergency");
            return false;
        }

        if (!limboPermission.CanSpeak)
        {
            Debug.Log("[BodyActionExecutor] Speak blocked by Limbo");
            return false;
        }

        return true;
    }

    private void EnterActingState()
    {
        if (stateController == null)
            return;

        stateBeforeAction = stateController.CurrentState;
        stateController.SetActing();
    }

    private void ExitActingState()
    {
        if (stateController == null)
            return;

        if (stateController.CurrentState == InteractionState.Emergency)
            return;

        stateController.SetRecovering();
    }

    private void FinishRecovering()
    {
        if (stateController == null)
            return;

        if (stateController.CurrentState == InteractionState.Emergency)
            return;

        if (stateBeforeAction == InteractionState.Tracking ||
            stateBeforeAction == InteractionState.TemporaryLost ||
            stateBeforeAction == InteractionState.FullyLost ||
            stateBeforeAction == InteractionState.Searching)
        {
            stateController.SetState(stateBeforeAction);
            return;
        }

        stateController.SetIdle();
    }

    private void PauseFaceTrackingTemporarily()
    {
        if (overrideComponent == null)
        {
            Debug.Log("[BodyActionExecutor] overrideComponent is null. Skip pause.");
            FinishRecovering();
            return;
        }

        if (resumeFaceTrackingCoroutine != null)
            StopCoroutine(resumeFaceTrackingCoroutine);

        resumeFaceTrackingCoroutine = StartCoroutine(ResumeFaceTrackingRoutine());
    }

    private IEnumerator ResumeFaceTrackingRoutine()
    {
        if (overrideComponent != null)
            overrideComponent.enabled = false;

        Debug.Log($"[BodyActionExecutor] FaceTracking paused {actionOverrideSeconds}s");

        yield return new WaitForSeconds(actionOverrideSeconds);

        NeuralEngineManager neuralEngine = overrideComponent as NeuralEngineManager;

        if (neuralEngine != null)
            neuralEngine.BeginSoftResume(lastOverrideYaw, lastOverridePitch);

        if (overrideComponent != null)
            overrideComponent.enabled = true;

        resumeFaceTrackingCoroutine = null;

        Debug.Log("[BodyActionExecutor] Override resumed");

        FinishRecovering();
    }
}