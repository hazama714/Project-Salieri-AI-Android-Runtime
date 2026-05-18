using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class InitialSetupGate : MonoBehaviour
{
    [Header("Boot Check Runner")]
    [SerializeField] private BootCheckRunner bootCheckRunner;

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Main";

    [Header("Wait")]
    [SerializeField] private float timeoutSeconds = 900f;
    [SerializeField] private float pollIntervalSeconds = 0.25f;

    [Header("UI")]
    [SerializeField] private Text statusText;

    private void Start()
    {
        StartCoroutine(WaitAndLoad());
    }

    private IEnumerator WaitAndLoad()
    {
        float elapsed = 0f;

        while (elapsed < timeoutSeconds)
        {
            if (bootCheckRunner == null)
            {
                UpdateStatus("BootCheckRunner Missing");
                Debug.LogError("[InitialSetupGate] bootCheckRunner is null.");
                yield break;
            }

            if (bootCheckRunner.IsFailed)
            {
                UpdateStatus("Startup Failed\n" + bootCheckRunner.LastError);
                Debug.LogError("[InitialSetupGate] startup failed: " + bootCheckRunner.LastError);
                yield break;
            }

            if (bootCheckRunner.IsCompleted)
            {
                UpdateStatus("Startup Complete\nLoading Main Scene...");
                yield return new WaitForSeconds(0.2f);
                LoadNextScene();
                yield break;
            }

            elapsed += pollIntervalSeconds;
            yield return new WaitForSeconds(pollIntervalSeconds);
        }

        UpdateStatus("Startup Timeout");
        Debug.LogError("[InitialSetupGate] startup timeout.");
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[InitialSetupGate] nextSceneName is empty.");
            return;
        }

        Debug.Log("[InitialSetupGate] Loading scene: " + nextSceneName);
        SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Single);
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
