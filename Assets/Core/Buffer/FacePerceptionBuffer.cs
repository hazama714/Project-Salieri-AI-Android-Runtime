using UnityEngine;

namespace SalieriAI.Core.Perception.Buffer
{
    public enum FacePerceptionState
    {
        Unknown,
        StableFound,
        TemporaryLost,
        FullyLost
    }

    public sealed class FacePerceptionBuffer : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private FaceDetector_OpenCV detector;

        [Header("Timing")]
        [SerializeField] private float foundConfirmSeconds = 0.20f;
        [SerializeField] private float temporaryLostSeconds = 2.0f;
        [SerializeField] private float fullyLostSeconds = 2.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = true;

        private FacePerceptionState state = FacePerceptionState.Unknown;

        private float rawFoundStartedAt = -1f;
        private float rawLostStartedAt = -1f;

        private bool previousRawFound;
        private FacePerceptionState previousState = FacePerceptionState.Unknown;

        private float stableFoundStartedAt = -1f;
        private float lostStartedAt = -1f;

        public float FaceVisibleDuration { get; private set; }
        public float NoFaceDuration { get; private set; }

        public FacePerceptionState State => state;
        public FacePerceptionState PreviousState => previousState;
        public bool IsStableFound => state == FacePerceptionState.StableFound;
        public bool IsTemporaryLost => state == FacePerceptionState.TemporaryLost;
        public bool IsFullyLost => state == FacePerceptionState.FullyLost;

        public bool BecameStableFound { get; private set; }
        public bool BecameTemporaryLost { get; private set; }
        public bool BecameFullyLost { get; private set; }

        private void Update()
        {
            BecameStableFound = false;
            BecameTemporaryLost = false;
            BecameFullyLost = false;

            if (detector == null)
                return;

            // ここは既存の FaceDetector_OpenCV の公開プロパティ名に合わせる
            bool rawFound = detector.HasFace;

            float now = Time.time;

            if (rawFound)
            {
                rawLostStartedAt = -1f;

                if (!previousRawFound)
                    rawFoundStartedAt = now;

                if (state != FacePerceptionState.StableFound)
                {
                    float foundDuration = now - rawFoundStartedAt;

                    if (foundDuration >= foundConfirmSeconds)
                        ChangeState(FacePerceptionState.StableFound);
                }
            }
            else
            {
                rawFoundStartedAt = -1f;

                if (previousRawFound || rawLostStartedAt < 0f)
                    rawLostStartedAt = now;

                float lostDuration = now - rawLostStartedAt;

                if (state == FacePerceptionState.StableFound)
                {
                    if (lostDuration >= temporaryLostSeconds)
                        ChangeState(FacePerceptionState.TemporaryLost);
                }
                else if (state == FacePerceptionState.TemporaryLost ||
                         state == FacePerceptionState.Unknown)
                {
                    if (lostDuration >= fullyLostSeconds)
                        ChangeState(FacePerceptionState.FullyLost);
                }
            }

            if (state == FacePerceptionState.StableFound)
            {
                if (stableFoundStartedAt < 0f)
                    stableFoundStartedAt = now;

                lostStartedAt = -1f;

                FaceVisibleDuration =
                    now - stableFoundStartedAt;

                NoFaceDuration = 0f;
            }
            else
            {
                if (lostStartedAt < 0f)
                    lostStartedAt = now;

                stableFoundStartedAt = -1f;

                NoFaceDuration =
                    now - lostStartedAt;

                FaceVisibleDuration = 0f;
            }
            
            previousRawFound = rawFound;
        }

        private void ChangeState(FacePerceptionState next)
        {
            if (state == next)
                return;

            previousState = state;
            state = next;

            BecameStableFound = next == FacePerceptionState.StableFound;
            BecameTemporaryLost = next == FacePerceptionState.TemporaryLost;
            BecameFullyLost = next == FacePerceptionState.FullyLost;

            if (!debugLog)
                return;

            Debug.Log($"[FacePerceptionBuffer] {previousState} -> {state}");
        }
    }
}