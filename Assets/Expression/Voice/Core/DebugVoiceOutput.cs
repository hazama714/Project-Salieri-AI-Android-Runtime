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
    public sealed class DebugVoiceOutput : MonoBehaviour, IVoiceOutput
    {
        public void Speak(string text, string speakerName)
        {
            UnityEngine.Debug.Log(
                $"[DebugVoiceOutput] speaker={speakerName} text={text}"
            );
        }
    }
}