using SalieriAI.AndroidLLM;
using UnityEngine;

public class LocalLLMHelloTest : MonoBehaviour
{
    [SerializeField] private LocalLLMRuntimeHost host;

    private async void Start()
    {
        Debug.Log("[LLM-Test] Start");

        if (host == null)
        {
            Debug.LogError("[LLM-Test] host is null");
            return;
        }

        string prompt =
@"<|user|>
Ç±ÇÒÇ…ÇøÇÕÅBíZÇ≠ï‘éñÇµÇƒÇ≠ÇæÇ≥Ç¢ÅB
<|assistant|>";

        Debug.Log("[LLM-Test] sending");

        string result =
            await host.GenerateSafeAsync(prompt);

        Debug.Log($"[LLM-Test] RESULT:\n{result}");
    }
}