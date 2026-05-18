namespace SalieriAI.Core.Affordance
{
    public sealed class BodyCapability
    {
        public bool HasNeck { get; set; } = true;
        public bool HasTorso { get; set; }
        public bool HasArm { get; set; }
        public bool CanMoveBase { get; set; }

        public static BodyCapability CurrentMinimum()
        {
            return new BodyCapability
            {
                HasNeck = true,
                HasTorso = false,
                HasArm = false,
                CanMoveBase = false
            };
        }
    }
}