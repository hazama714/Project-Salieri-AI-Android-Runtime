using UnityEngine;
using System;
using System.IO;
using System.Text;

public class LogToFile : MonoBehaviour
{
    private string logPath;

    private void Awake()
    {
        logPath = Path.Combine(Application.persistentDataPath, "log.txt");

        Application.logMessageReceived += HandleLog;

        File.WriteAllText(
            logPath,
            "=== Project Salieri AI LOG START ===\n" +
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\n" +
            "Path: " + logPath + "\n\n",
            Encoding.UTF8
        );

        Debug.Log("[LogToFile] Started");
        Debug.Log("[LogToFile] Path: " + logPath);
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        try
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            string line = $"[{time}][{type}] {logString}\n";

            File.AppendAllText(logPath, line, Encoding.UTF8);

            if (type == LogType.Error || type == LogType.Exception)
            {
                File.AppendAllText(logPath, stackTrace + "\n", Encoding.UTF8);
            }
        }
        catch
        {
            // ログ保存失敗でさらにログを出すと無限ループになるので何もしない
        }
    }
}