using System;

namespace SalieriAI.Persona
{
    [Serializable]
    public sealed class PersonaBaseProfile
    {
        public string name;
        public string identity;
        public string[] personality;
        public string[] behavior_tendencies;
    }
}