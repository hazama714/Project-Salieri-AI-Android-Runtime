/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System;

namespace SalieriAI.Core.LLM.Common
{
    [Serializable]
    public sealed class LLMSpeechDecision
    {
        public string speech = "";
        public string reason = "";
        public float confidence = 0f;

        public bool HasSpeech()
        {
            return !string.IsNullOrWhiteSpace(speech);
        }

        public void Normalize()
        {
            if (speech == null)
                speech = "";

            if (reason == null)
                reason = "";

            if (confidence < 0f)
                confidence = 0f;

            if (confidence > 1f)
                confidence = 1f;
        }

        public static LLMSpeechDecision Empty(string reason = "empty")
        {
            return new LLMSpeechDecision
            {
                speech = "",
                reason = reason,
                confidence = 0f
            };
        }
    }
}