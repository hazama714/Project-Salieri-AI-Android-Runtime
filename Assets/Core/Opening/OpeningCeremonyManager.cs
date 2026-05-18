using System.Collections;
using UnityEngine;
using SalieriAI.Core.State;

namespace SalieriAI.Core.Opening
{
    public sealed class OpeningCeremonyManager : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private InteractionStateController stateController;

        [SerializeField] private NeckController neckController;

        [Header("Timing")]
        [SerializeField] private float bootWaitSeconds = 1.0f;
        [SerializeField] private float servoNeutralWaitSeconds = 1.0f;
        [SerializeField] private float cameraWarmupSeconds = 2.0f;
        [SerializeField] private float openCvWarmupSeconds = 2.0f;

        [Header("Servo Neutral")]
        [SerializeField] private bool moveNeckToNeutral = true;

        private bool isRunning;
        private bool isCompleted;

        public bool IsCompleted => isCompleted;

        private void Awake()
        {
            if (stateController == null)
                stateController = FindObjectOfType<InteractionStateController>();

            if (stateController != null)
                stateController.SetState(InteractionState.Booting);
        }

        private void Start()
        {
            StartOpening();
        }

        public void StartOpening()
        {
            if (isRunning)
                return;

            StartCoroutine(OpeningRoutine());
        }

        private IEnumerator OpeningRoutine()
        {
            isRunning = true;
            isCompleted = false;

            Debug.Log("[OpeningCeremony] START");

            if (stateController == null)
            {
                Debug.LogError("[OpeningCeremony] stateController is null");
                yield break;
            }

            // BootèÛë‘Ç÷
            stateController.SetState(InteractionState.Booting);

            yield return new WaitForSeconds(bootWaitSeconds);

            // éÒèâä˙épê®
            if (moveNeckToNeutral && neckController != null)
            {
                Debug.Log("[OpeningCeremony] Move neck neutral");

                neckController.ReturnCenterForOpening(0.25f);
            }
            else
            {
                Debug.LogWarning("[OpeningCeremony] Neck neutral skipped");
            }

            yield return new WaitForSeconds(servoNeutralWaitSeconds);

            // Camera warmup
            Debug.Log("[OpeningCeremony] Camera warmup");
            yield return new WaitForSeconds(cameraWarmupSeconds);

            // OpenCV warmup
            Debug.Log("[OpeningCeremony] OpenCV warmup");
            yield return new WaitForSeconds(openCvWarmupSeconds);

            // RuntimeäJén
            stateController.SetIdle();

            Debug.Log("[OpeningCeremony] Runtime State = Idle");

            isCompleted = true;
            isRunning = false;

            Debug.Log("[OpeningCeremony] COMPLETE");
        }
    }
}