/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using UnityEngine;

namespace SalieriAI.Expression.Voice
{
    public sealed class VoiceController : MonoBehaviour
    {
        [Header("Output")]
        [SerializeField] private MonoBehaviour voiceOutputBehaviour;

        private IVoiceOutput VoiceOutput =>
            voiceOutputBehaviour as IVoiceOutput;

        public void Speak(string text, string speakerName)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (VoiceOutput == null)
            {
                UnityEngine.Debug.LogWarning("[VoiceController] VoiceOutput is null");
                return;
            }

            VoiceOutput.Speak(text, speakerName);
        }
    }
}