/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;
using UnityEngine;

[Serializable]
public sealed class LLMActionDecision
{
    public string action = "none";
    public string speech = "";
    public string reason = "";
    public float confidence = 0f;

    public bool IsNone()
    {
        return string.IsNullOrWhiteSpace(action) ||
               action == "none";
    }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(action))
            action = "none";

        if (speech == null)
            speech = "";

        if (reason == null)
            reason = "";

        confidence = Mathf.Clamp01(confidence);
    }

    public static LLMActionDecision SafeDefault(string reason = "fallback")
    {
        return new LLMActionDecision
        {
            action = "none",
            speech = "",
            reason = reason,
            confidence = 0f
        };
    }
}