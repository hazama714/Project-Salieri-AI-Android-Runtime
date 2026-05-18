using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class InitialAssetInstaller : MonoBehaviour
{
    public const string PrefKeyPrefix = "Salieri.InitialAsset.";

    [Serializable]
    public class InstallItem
    {
        public string id;

        [Header("Source")]
        public string streamingAssetsRelativePath;

        [Header("Destination")]
        public string destinationRelativePath;

        [Header("Validation")]
        public long minBytes = 1024;
        public string requiredExtension;

        [Header("Options")]
        public bool required = true;
        public bool overwrite = false;
    }

    [Header("Install Items")]
    public List<InstallItem> installItems = new List<InstallItem>();

    [Header("Options")]
    public bool runOnStart = true;

    [Range(0, 5)]
    public int maxRetry = 2;

    [Header("UI")]
    public Text progressText;

    public bool IsCompleted { get; private set; }
    public bool IsFailed { get; private set; }
    public string LastError { get; private set; }

    private void Start()
    {
        if (runOnStart)
            StartCoroutine(EnsureAllInstalled());
    }

    public IEnumerator EnsureAllInstalled()
    {
        IsCompleted = false;
        IsFailed = false;
        LastError = "";

        for (int i = 0; i < installItems.Count; i++)
        {
            InstallItem item = installItems[i];

            if (item == null || string.IsNullOrEmpty(item.id))
                continue;

            UpdateStatus(
                $"Initial Setup...\n" +
                $"{item.id} ({i + 1}/{installItems.Count})"
            );

            yield return EnsureInstalled(item);

            if (IsFailed && item.required)
                yield break;
        }

        IsCompleted = true;

        UpdateStatus("Initial Setup Complete");

        Debug.Log("[InitialAssetInstaller] All install items completed.");
    }

    private IEnumerator EnsureInstalled(InstallItem item)
    {
        string source = GetStreamingAssetsPath(item.streamingAssetsRelativePath);
        string dest = GetPersistentPath(item.destinationRelativePath);

        Debug.Log($"[InitialAssetInstaller] Install check: {item.id}");
        Debug.Log($"[InitialAssetInstaller] SA='{source}'");
        Debug.Log($"[InitialAssetInstaller] PD='{dest}'");

        if (!item.overwrite && ValidateFile(dest, item, out string okReason))
        {
            SaveInstalledPath(item, dest);

            Debug.Log(
                $"[InitialAssetInstaller] Already installed: {item.id} / {okReason}"
            );

            yield break;
        }

        TryDeleteQuiet(dest);

        for (int attempt = 0; attempt <= maxRetry; attempt++)
        {
            yield return CopyFromStreamingAssets(item, source, dest);

            if (ValidateFile(dest, item, out string reason))
            {
                SaveInstalledPath(item, dest);

                Debug.Log(
                    $"[InitialAssetInstaller] Installed OK: {item.id} / {reason}"
                );

                yield break;
            }

            Debug.LogWarning(
                $"[InitialAssetInstaller] Validate failed: {item.id} / {reason}"
            );

            TryDeleteQuiet(dest);
        }

        IsFailed = true;
        LastError = $"Install failed: {item.id}";

        Debug.LogError("[InitialAssetInstaller] " + LastError);

        UpdateStatus(
            "Initial Setup Failed\n" +
            item.id
        );
    }

    private IEnumerator CopyFromStreamingAssets(
        InstallItem item,
        string source,
        string dest
    )
    {
        string dir = Path.GetDirectoryName(dest);

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        long totalBytes = 0;

        using (UnityWebRequest head = UnityWebRequest.Head(source))
        {
            yield return head.SendWebRequest();

            if (head.result == UnityWebRequest.Result.Success)
            {
                long.TryParse(
                    head.GetResponseHeader("Content-Length"),
                    out totalBytes
                );
            }
        }

        using (UnityWebRequest uwr = UnityWebRequest.Get(source))
        {
            DownloadHandlerFile handler = new DownloadHandlerFile(dest)
            {
                removeFileOnAbort = true
            };

            uwr.downloadHandler = handler;

            float start = Time.realtimeSinceStartup;

            UnityWebRequestAsyncOperation op = uwr.SendWebRequest();

            while (!op.isDone)
            {
                long downloaded = (long)uwr.downloadedBytes;

                float elapsed =
                    Mathf.Max(0.001f, Time.realtimeSinceStartup - start);

                double copiedMb = downloaded / 1024.0 / 1024.0;
                double totalMb = totalBytes / 1024.0 / 1024.0;
                double speed = copiedMb / elapsed;

                if (progressText != null)
                {
                    if (totalBytes > 0)
                    {
                        int pct = Mathf.Clamp(
                            Mathf.RoundToInt(
                                (float)(downloaded * 100.0 / totalBytes)
                            ),
                            0,
                            100
                        );

                        progressText.text =
                            $"Initial Setup...\n" +
                            $"{item.id}\n" +
                            $"{pct}%\n" +
                            $"{copiedMb:0} / {totalMb:0} MB\n" +
                            $"Speed: {speed:0.0} MB/s";
                    }
                    else
                    {
                        progressText.text =
                            $"Initial Setup...\n" +
                            $"{item.id}\n" +
                            $"{copiedMb:0} MB\n" +
                            $"Speed: {speed:0.0} MB/s";
                    }
                }

                yield return null;
            }

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    $"[InitialAssetInstaller] Copy failed: {item.id} / {uwr.error}"
                );
            }
        }
    }

    private bool ValidateFile(
        string path,
        InstallItem item,
        out string reason
    )
    {
        reason = "";

        if (!File.Exists(path))
        {
            reason = "file not found";
            return false;
        }

        FileInfo info = new FileInfo(path);

        if (info.Length < item.minBytes)
        {
            reason = $"too small: {info.Length} bytes";
            return false;
        }

        if (!string.IsNullOrEmpty(item.requiredExtension))
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            string req = item.requiredExtension.ToLowerInvariant();

            if (!req.StartsWith("."))
                req = "." + req;

            if (ext != req)
            {
                reason = $"extension mismatch: {ext} != {req}";
                return false;
            }
        }

        reason = $"OK size={info.Length}";
        return true;
    }

    private void SaveInstalledPath(InstallItem item, string path)
    {
        PlayerPrefs.SetString(
            PrefKeyPrefix + item.id + ".Path",
            path
        );

        PlayerPrefs.SetString(
            PrefKeyPrefix + item.id + ".Time",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        );

        PlayerPrefs.Save();
    }

    public static string GetInstalledPath(string id)
    {
        return PlayerPrefs.GetString(
            PrefKeyPrefix + id + ".Path",
            ""
        );
    }

    private static string GetStreamingAssetsPath(string relative)
    {
        return Path.Combine(
            Application.streamingAssetsPath,
            relative
        ).Replace('\\', '/');
    }

    private static string GetPersistentPath(string relative)
    {
        return Path.Combine(
            Application.persistentDataPath,
            relative
        ).Replace('\\', '/');
    }

    private static void TryDeleteQuiet(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[InitialAssetInstaller] Delete failed: " + ex.Message
            );
        }
    }

    private void UpdateStatus(string text)
    {
        if (progressText != null)
            progressText.text = text;
    }
}