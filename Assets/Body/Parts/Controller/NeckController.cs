/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using UnityEngine;
using SalieriAI.Runtime;

public class NeckController : MonoBehaviour
{
    [Header("Sender")]
    public MonoBehaviour senderBehaviour;

    [Header("Runtime")]
    [SerializeField] private RuntimeConnectionSettings runtimeSettings;

    private ICommandSender sender;

    [Header("Servos")]
    public ServoControlUnit yawServo = new ServoControlUnit
    {
        servoIndex = 0,
        servoName = "Neck Yaw",
        minAngle = 0,
        maxAngle = 180
    };

    public ServoControlUnit pitchServo = new ServoControlUnit
    {
        servoIndex = 1,
        servoName = "Neck Pitch",
        minAngle = 45,
        maxAngle = 135
    };

    [Header("Center")]
    [SerializeField] private float yawCenterAngle = 90f;
    [SerializeField] private float pitchCenterAngle = 90f;

    [Header("Relative Limit")]
    [SerializeField] private float yawLimit = 45f;
    [SerializeField] private float pitchLimit = 30f;

    [Header("Smooth Follow")]
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float nearTargetSpeed = 2f;
    [SerializeField] private float slowDownAngle = 6f;
    [SerializeField] private float stopDeadZone = 0.8f;

    [Header("Send Control")]
    [SerializeField] private float sendInterval = 0.08f;
    [SerializeField] private float minSendDelta = 1f;

    private float targetYaw;
    private float targetPitch;

    private float currentYaw;
    private float currentPitch;

    private int lastSentYawServoAngle = int.MinValue;
    private int lastSentPitchServoAngle = int.MinValue;

    private float sendTimer;

    private bool initialized;

    private bool openingSmoothActive;
    private float baseFollowSpeed;
    private float baseNearTargetSpeed;

    private void Awake()
    {
        if (runtimeSettings == null)
        {
            runtimeSettings = FindObjectOfType<RuntimeConnectionSettings>();
        }

        sender = senderBehaviour as ICommandSender;

        if (sender == null)
        {
            Debug.LogError(
                "[NeckController] senderBehaviour does not implement ICommandSender."
            );

            enabled = false;
            return;
        }

        yawServo.Initialize(sender, runtimeSettings);
        pitchServo.Initialize(sender, runtimeSettings);

        baseFollowSpeed = followSpeed;
        baseNearTargetSpeed = nearTargetSpeed;

        targetYaw = 0f;
        targetPitch = 0f;
        currentYaw = 0f;
        currentPitch = 0f;

        SendCurrentAngles(force: true);

        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
            return;

        currentYaw = SmoothAxis(currentYaw, targetYaw);
        currentPitch = SmoothAxis(currentPitch, targetPitch);

        sendTimer += Time.deltaTime;

        if (sendTimer < sendInterval)
            return;

        sendTimer = 0f;

        SendCurrentAngles(force: false);

        if (openingSmoothActive &&
            Mathf.Abs(currentYaw - targetYaw) < stopDeadZone &&
            Mathf.Abs(currentPitch - targetPitch) < stopDeadZone)
        {
            openingSmoothActive = false;

            followSpeed = baseFollowSpeed;
            nearTargetSpeed = baseNearTargetSpeed;

            Debug.Log("[NeckController] Opening smooth completed. Speed restored.");
        }
    }

    public void LookAt(float yaw, float pitch)
    {
        targetYaw = Mathf.Clamp(yaw, -yawLimit, yawLimit);
        targetPitch = Mathf.Clamp(pitch, -pitchLimit, pitchLimit);
    }

    private float SmoothAxis(float current, float target)
    {
        float diff = target - current;

        if (Mathf.Abs(diff) < stopDeadZone)
            return target;

        float speed = Mathf.Abs(diff) < slowDownAngle
            ? nearTargetSpeed
            : followSpeed;

        return Mathf.Lerp(current, target, Time.deltaTime * speed);
    }

    private void SendCurrentAngles(bool force)
    {
        int yawServoAngle = Mathf.RoundToInt(yawCenterAngle + currentYaw);
        int pitchServoAngle = Mathf.RoundToInt(pitchCenterAngle + currentPitch);

        if (force || Mathf.Abs(yawServoAngle - lastSentYawServoAngle) >= minSendDelta)
        {
            yawServo.SetAngle(yawServoAngle);
            lastSentYawServoAngle = yawServoAngle;
        }

        if (force || Mathf.Abs(pitchServoAngle - lastSentPitchServoAngle) >= minSendDelta)
        {
            pitchServo.SetAngle(pitchServoAngle);
            lastSentPitchServoAngle = pitchServoAngle;
        }
    }

    public void SetYaw(float yaw)
    {
        LookAt(yaw, targetPitch);
    }

    public void SetPitch(float pitch)
    {
        LookAt(targetYaw, pitch);
    }

    public void SetYawServoRaw(int angle)
    {
        yawServo.SetAngle(angle);
        lastSentYawServoAngle = angle;
    }

    public void SetPitchServoRaw(int angle)
    {
        pitchServo.SetAngle(angle);
        lastSentPitchServoAngle = angle;
    }

    public void ReturnCenter()
    {
        openingSmoothActive = false;

        followSpeed = baseFollowSpeed;
        nearTargetSpeed = baseNearTargetSpeed;

        targetYaw = 0f;
        targetPitch = 0f;

        Debug.Log("[NeckController] ReturnCenter");
    }

    public void ReturnCenterForOpening(float speedMultiplier = 0.25f)
    {
        openingSmoothActive = true;

        followSpeed = baseFollowSpeed * Mathf.Clamp01(speedMultiplier);
        nearTargetSpeed = baseNearTargetSpeed * Mathf.Clamp01(speedMultiplier);

        targetYaw = 0f;
        targetPitch = 0f;

        Debug.Log(
            $"[NeckController] ReturnCenterForOpening speedMultiplier:{speedMultiplier}"
        );
    }
}