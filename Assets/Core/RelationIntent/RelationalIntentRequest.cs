namespace SalieriAI.Core.RelationIntent
{
    public sealed class RelationalIntentRequest
    {
        public RelationalIntentType IntentType { get; }
        public string TargetId { get; }
        public float Strength { get; }

        public RelationalIntentRequest(
            RelationalIntentType intentType,
            string targetId,
            float strength = 1.0f)
        {
            IntentType = intentType;
            TargetId = targetId;
            Strength = strength;
        }

        public static RelationalIntentRequest None()
        {
            return new RelationalIntentRequest(
                RelationalIntentType.None,
                string.Empty,
                0f
            );
        }
    }
}