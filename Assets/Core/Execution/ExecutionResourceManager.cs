using UnityEngine;
using SalieriAI.Core.State;

namespace SalieriAI.Core.Execution
{
    public sealed class ExecutionResourceManager : MonoBehaviour
    {
        [Header("Profiles")]
        [SerializeField]
        private StateExecutionProfile[] profiles;

        [Header("Tracking")]
        [SerializeField]
        private MonoBehaviour faceTrackingBehaviour;

        private StateExecutionProfile currentProfile;

        public void ApplyProfile(InteractionState state)
        {
            Debug.Log($"[ExecutionResourceManager] ApplyProfile: {state}");

            currentProfile = null;

            if (profiles == null || profiles.Length == 0)
            {
                Debug.LogWarning("[ExecutionResourceManager] profiles is empty.");
                return;
            }

            for (int i = 0; i < profiles.Length; i++)
            {
                if (profiles[i] != null && profiles[i].state == state)
                {
                    currentProfile = profiles[i];
                    break;
                }
            }

            if (currentProfile == null)
            {
                Debug.LogWarning($"[ExecutionResourceManager] Profile not found: {state}");
                return;
            }

            Debug.Log(
                $"[ExecutionResourceManager] Profile found: {state}, " +
                $"FaceTracking={currentProfile.faceTracking}"
            );

            ApplyTracking();
        }

        private void ApplyTracking()
        {
            Debug.Log("[ExecutionResourceManager] ApplyTracking ENTER");

            if (faceTrackingBehaviour == null)
            {
                Debug.LogWarning("[ExecutionResourceManager] faceTrackingBehaviour is null.");
                return;
            }

            bool enableTracking =
                currentProfile.faceTracking != ExecutionLevel.Off;

            faceTrackingBehaviour.enabled = enableTracking;

            Debug.Log($"[ExecutionResourceManager] FaceTracking = {enableTracking}");
        }
    }
}