/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

namespace SalieriAI.Expression.Voice
{
    public interface IVoiceOutput
    {
        void Speak(string text, string speakerName);
    }
}