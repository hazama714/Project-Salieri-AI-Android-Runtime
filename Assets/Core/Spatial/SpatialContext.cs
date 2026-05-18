namespace SalieriAI.Core.Spatial
{
    public sealed class SpatialContext
    {
        public bool HasTarget { get; }
        public string TargetId { get; }
        public SpatialDirection Direction { get; }
        public TargetDistance Distance { get; }
        public bool IsReachable { get; }

        public SpatialContext(
            bool hasTarget,
            string targetId,
            SpatialDirection direction,
            TargetDistance distance,
            bool isReachable)
        {
            HasTarget = hasTarget;
            TargetId = targetId;
            Direction = direction;
            Distance = distance;
            IsReachable = isReachable;
        }

        public static SpatialContext NoTarget()
        {
            return new SpatialContext(
                false,
                string.Empty,
                SpatialDirection.Unknown,
                TargetDistance.Unknown,
                false
            );
        }
    }
}