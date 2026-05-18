using UnityEngine;

namespace SalieriAI.Core.LLM
{
    public enum LLMProfileType
    {
        Action,
        Speech
    }

    [System.Serializable]
    public sealed class LLMGenerationProfile
    {
        public int maxTokens = 2;
        public float temperature = 0.0f;
        public float topP = 0.1f;
        public int topK = 1;
        public float repeatPenalty = 1.0f;
    }

    public sealed class LLMProfileManager : MonoBehaviour
    {
        [Header("Profiles")]
        [SerializeField]
        private LLMGenerationProfile actionProfile =
            new LLMGenerationProfile
            {
                maxTokens = 2,
                temperature = 0.0f,
                topP = 0.1f,
                topK = 1,
                repeatPenalty = 1.0f
            };

        [SerializeField]
        private LLMGenerationProfile speechProfile =
            new LLMGenerationProfile
            {
                maxTokens = 48,
                temperature = 0.7f,
                topP = 0.9f,
                topK = 40,
                repeatPenalty = 1.1f
            };

        [Header("Debug")]
        [SerializeField] private LLMProfileType currentProfileType = LLMProfileType.Action;

        public LLMGenerationProfile CurrentProfile { get; private set; }

        private void Awake()
        {
            ApplyActionProfile();
        }

        public void ApplyActionProfile()
        {
            currentProfileType = LLMProfileType.Action;
            CurrentProfile = actionProfile;

            Debug.Log("[LLMProfileManager] ApplyActionProfile");
        }

        public void ApplySpeechProfile()
        {
            currentProfileType = LLMProfileType.Speech;
            CurrentProfile = speechProfile;

            Debug.Log("[LLMProfileManager] ApplySpeechProfile");
        }

        public int CurrentMaxTokens =>
            CurrentProfile != null ? CurrentProfile.maxTokens : 2;

        public float CurrentTemperature =>
            CurrentProfile != null ? CurrentProfile.temperature : 0.0f;

        public float CurrentTopP =>
            CurrentProfile != null ? CurrentProfile.topP : 0.1f;

        public int CurrentTopK =>
            CurrentProfile != null ? CurrentProfile.topK : 1;

        public float CurrentRepeatPenalty =>
            CurrentProfile != null ? CurrentProfile.repeatPenalty : 1.0f;
    }
}