using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BootCheckRunner : MonoBehaviour
{
    [Header("Installer")]
    [SerializeField] private InitialAssetInstaller installer;

    [Header("Boot Checks")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private List<BootCheckBase> checks = new List<BootCheckBase>();

    [Header("Wait")]
    [SerializeField] private float installerTimeoutSeconds = 600f;
    [SerializeField] private float pollIntervalSeconds = 0.25f;

    [Header("UI")]
    [SerializeField] private Text statusText;

    public bool IsCompleted { get; private set; }
    public bool IsFailed { get; private set; }
    public string LastError { get; private set; }

    private void Start()
    {
        if (runOnStart)
            StartCoroutine(RunAll());
    }

    public IEnumerator RunAll()
    {
        IsCompleted = false;
        IsFailed = false;
        LastError = string.Empty;

        yield return WaitForInstaller();

        if (IsFailed)
            yield break;

        for (int i = 0; i < checks.Count; i++)
        {
            BootCheckBase check = checks[i];

            if (check == null)
                continue;

            UpdateStatus($"Boot Check...\n{check.DisplayName} ({i + 1}/{checks.Count})");

            yield return check.Run();

            if (!check.IsSuccess && check.Required)
            {
                IsFailed = true;
                LastError = $"{check.DisplayName}: {check.Message}";
                UpdateStatus("Boot Check Failed\n" + LastError);
                Debug.LogError("[BootCheckRunner] " + LastError);
                yield break;
            }
        }

        IsCompleted = true;
        UpdateStatus("Boot Check Complete");
        Debug.Log("[BootCheckRunner] All boot checks completed.");
    }

    private IEnumerator WaitForInstaller()
    {
        if (installer == null)
        {
            PassInstallerMissingAsNoInstallMode();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < installerTimeoutSeconds)
        {
            if (installer.IsFailed)
            {
                IsFailed = true;
                LastError = installer.LastError;
                UpdateStatus("Initial Install Failed\n" + LastError);
                yield break;
            }

            if (installer.IsCompleted)
                yield break;

            elapsed += pollIntervalSeconds;
            yield return new WaitForSeconds(pollIntervalSeconds);
        }

        IsFailed = true;
        LastError = "Initial asset install timeout.";
        UpdateStatus("Initial Install Timeout");
    }

    private void PassInstallerMissingAsNoInstallMode()
    {
        Debug.LogWarning("[BootCheckRunner] installer is null. Run checks without install wait.");
    }

    private void UpdateStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }
}
