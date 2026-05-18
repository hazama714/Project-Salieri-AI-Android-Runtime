/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Threading.Tasks;

namespace SalieriAI.Core.LLM.Common
{
    public interface ILLMSpeechProvider
    {
        Task<LLMSpeechDecision> DecideSpeechAsync(SelfStateSummary summary, LLMActionDecision actionDecision);
    }
}