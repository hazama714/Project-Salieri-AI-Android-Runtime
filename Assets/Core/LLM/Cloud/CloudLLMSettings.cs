using UnityEngine;

namespace SalieriAI.CloudLLM
{
    [CreateAssetMenu(
        fileName = "CloudLLMSettings",
        menuName = "SalieriAI/CloudLLMSettings"
    )]
    public sealed class CloudLLMSettings : ScriptableObject
    {
        [Header("OpenAI")]
        public string apiKey;

        [Header("Model")]
        public string model = "gpt-5.4-mini";

        [Header("Request")]
        public int timeoutSeconds = 20;

        public int maxOutputTokens = 2;
    }
}