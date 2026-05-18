/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using UnityEngine;
using SalieriAI.Runtime;

[System.Serializable]
public class ServoControlUnit
{
    [Header("Servo")]
    public int servoIndex = 0;
    public string servoName = "Servo";

    [Header("Limit")]
    public int minAngle = 0;
    public int maxAngle = 180;

    [Header("Correction")]
    public bool invert = false;
    public int offset = 0;

    private ICommandSender sender;
    private RuntimeConnectionSettings runtimeSettings;

    private int lastSentAngle = -999;

    public void Initialize(
        ICommandSender commandSender,
        RuntimeConnectionSettings settings)
    {
        sender = commandSender;
        runtimeSettings = settings;

        Debug.Log(
            $"[ServoControlUnit][INIT] " +
            $"{servoName} ID:{servoIndex} Instance:{GetHashCode()}"
        );
    }

    public void SetAngle(int inputAngle)
    {
        int finalAngle = inputAngle;

        if (invert)
        {
            finalAngle = 180 - finalAngle;
        }

        finalAngle += offset;
        finalAngle = Mathf.Clamp(finalAngle, minAngle, maxAngle);

        Debug.Log(
            $"[ServoControlUnit][CALC] " +
            $"{servoName} ID:{servoIndex} Input:{inputAngle} Final:{finalAngle}"
        );

        if (runtimeSettings != null &&
            !runtimeSettings.useServo)
        {
            Debug.Log(
                $"[ServoControlUnit][SEND_SKIP_DISABLED] " +
                $"{servoName} ID:{servoIndex} Angle:{finalAngle}"
            );

            return;
        }

        if (sender == null)
        {
            Debug.LogWarning(
                $"[ServoControlUnit][ERROR] " +
                $"sender is null / {servoName} ID:{servoIndex}"
            );

            return;
        }

        if (lastSentAngle == finalAngle)
        {
            Debug.Log(
                $"[ServoControlUnit][SEND_SKIP_DUPLICATE] " +
                $"{servoName} ID:{servoIndex} Angle:{finalAngle}"
            );

            return;
        }

        lastSentAngle = finalAngle;

        Debug.Log(
            $"[ServoControlUnit][SEND_CALL] " +
            $"{servoName} ID:{servoIndex} Angle:{finalAngle}"
        );

        sender.SendServo(servoIndex, finalAngle);
    }
}