namespace SalieriAI.Core.Affordance
{
    public sealed class AffordanceCandidate
    {
        public AffordanceType Type { get; }
        public string ActionId { get; }
        public string Reason { get; }

        public AffordanceCandidate(
            AffordanceType type,
            string actionId,
            string reason)
        {
            Type = type;
            ActionId = actionId;
            Reason = reason;
        }

        public static AffordanceCandidate None(string reason)
        {
            return new AffordanceCandidate(
                AffordanceType.None,
                "none",
                reason
            );
        }
    }
}