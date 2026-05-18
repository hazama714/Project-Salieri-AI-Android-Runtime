using System;

namespace SalieriAI.Persona
{
    [Serializable]
    public sealed class SpeechStyleProfile
    {
        public string dialect;
        public string tone;
        public string length;
        public string[] rules;
        public string[] examples;
    }
}