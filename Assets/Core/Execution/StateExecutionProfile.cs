using SalieriAI.Core.State;

namespace SalieriAI.Core.Execution
{
    [System.Serializable]
    public sealed class StateExecutionProfile
    {
        public InteractionState state;

        public ExecutionLevel camera = ExecutionLevel.Normal;
        public ExecutionLevel faceTracking = ExecutionLevel.Normal;
        public ExecutionLevel servoTracking = ExecutionLevel.Normal;

        public ExecutionLevel llm = ExecutionLevel.Normal;
        public ExecutionLevel voice = ExecutionLevel.Normal;
        public ExecutionLevel expression = ExecutionLevel.Normal;
    }
}