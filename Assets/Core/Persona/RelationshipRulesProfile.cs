using System;

namespace SalieriAI.Persona
{
    [Serializable]
    public sealed class RelationshipRulesProfile
    {
        public string default_user_name;
        public string self_pronoun;
        public string relationship;
        public string[] rules;
    }
}