using UnityEngine;

namespace SalieriAI.Core.State
{
    public sealed class InteractionStateController : MonoBehaviour
    {
        [SerializeField] private InteractionState currentState = InteractionState.Booting;

        public InteractionState CurrentState => currentState;

        public bool IsBusy =>
            currentState == InteractionState.Thinking ||
            currentState == InteractionState.Speaking ||
            currentState == InteractionState.Acting ||
            currentState == InteractionState.Recovering;

        public void SetState(InteractionState next)
        {
            if (currentState == next)
                return;

            Debug.Log($"[InteractionStateController] {currentState} -> {next}");
            currentState = next;
        }

        public void SetIdle()
        {
            SetState(InteractionState.Idle);
        }

        public void SetTracking()
        {
            SetState(InteractionState.Tracking);
        }

        public void SetTemporaryLost()
        {
            SetState(InteractionState.TemporaryLost);
        }

        public void SetFullyLost()
        {
            SetState(InteractionState.FullyLost);
        }

        public void SetSearching()
        {
            SetState(InteractionState.Searching);
        }

        public void SetThinking()
        {
            SetState(InteractionState.Thinking);
        }

        public void SetSpeaking()
        {
            SetState(InteractionState.Speaking);
        }

        public void SetListening()
        {
            SetState(InteractionState.Listening);
        }

        public void SetActing()
        {
            SetState(InteractionState.Acting);
        }

        public void SetRecovering()
        {
            SetState(InteractionState.Recovering);
        }

        public void SetEmergency()
        {
            SetState(InteractionState.Emergency);
        }
    }
}