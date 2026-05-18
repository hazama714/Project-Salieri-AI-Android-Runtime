using UnityEngine;

namespace SalieriAI.Core.Limbo
{
    public sealed class LimboPermission : MonoBehaviour
    {
        [Header("Runtime Permission")]
        [SerializeField] private bool canThink;
        [SerializeField] private bool canTrackFace;
        [SerializeField] private bool canMoveServo;
        [SerializeField] private bool canStartAction;
        [SerializeField] private bool canSpeak;
        [SerializeField] private bool canSearch;
        [SerializeField] private bool canInterrupt;
        [SerializeField] private bool isEmergencyMode;

        public bool CanThink => canThink;
        public bool CanTrackFace => canTrackFace;
        public bool CanMoveServo => canMoveServo;
        public bool CanStartAction => canStartAction;
        public bool CanSpeak => canSpeak;
        public bool CanSearch => canSearch;
        public bool CanInterrupt => canInterrupt;
        public bool IsEmergencyMode => isEmergencyMode;

        private void Awake()
        {
            LockAll();
        }

        public void LockAll()
        {
            canThink = false;
            canTrackFace = false;
            canMoveServo = false;
            canStartAction = false;
            canSpeak = false;
            canSearch = false;
            canInterrupt = false;
            isEmergencyMode = false;

            Debug.Log("[LimboPermission] LockAll");
        }

        public void AllowServo()
        {
            canMoveServo = true;
            Debug.Log("[LimboPermission] AllowServo");
        }

        public void AllowFaceTracking()
        {
            canTrackFace = true;
            Debug.Log("[LimboPermission] AllowFaceTracking");
        }

        public void DenyFaceTracking()
        {
            canTrackFace = false;
            Debug.Log("[LimboPermission] DenyFaceTracking");
        }

        public void AllowSpeak()
        {
            canSpeak = true;
            Debug.Log("[LimboPermission] AllowSpeak");
        }

        public void AllowThinking()
        {
            canThink = true;
            canStartAction = true;

            Debug.Log("[LimboPermission] AllowThinking / AllowAction");
        }

        public void AllowSearch()
        {
            canSearch = true;
            Debug.Log("[LimboPermission] AllowSearch");
        }

        public void AllowInterrupt()
        {
            canInterrupt = true;
            Debug.Log("[LimboPermission] AllowInterrupt");
        }

        public void EnterRuntime()
        {
            canThink = true;
            canTrackFace = true;
            canMoveServo = true;
            canStartAction = true;
            canSpeak = true;
            canSearch = true;
            canInterrupt = true;
            isEmergencyMode = false;

            Debug.Log("[LimboPermission] EnterRuntime");
        }

        public void EnterEmergencyMode()
        {
            isEmergencyMode = true;

            canThink = false;
            canTrackFace = false;
            canMoveServo = false;
            canStartAction = false;
            canSpeak = false;
            canSearch = false;
            canInterrupt = false;

            Debug.Log("[LimboPermission] EnterEmergencyMode");
        }

        public void ExitEmergencyMode()
        {
            isEmergencyMode = false;
            Debug.Log("[LimboPermission] ExitEmergencyMode");
        }
    }
}