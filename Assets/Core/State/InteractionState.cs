namespace SalieriAI.Core.State
{
    public enum InteractionState
    {
        Booting,
        Idle,
        Tracking,
        TemporaryLost,
        FullyLost,
        Searching,
        Thinking,
        Speaking,
        Listening,
        Acting,
        Recovering,
        Emergency
    }
}