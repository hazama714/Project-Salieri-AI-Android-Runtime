using System.Collections;
using UnityEngine;

public abstract class BootCheckBase : MonoBehaviour
{
    [Header("Boot Check")]
    [SerializeField] private string displayName;
    [SerializeField] private bool required = true;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? GetType().Name : displayName;
    public bool Required => required;
    public bool IsCompleted { get; private set; }
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }

    public IEnumerator Run()
    {
        IsCompleted = false;
        IsSuccess = false;
        Message = string.Empty;

        Debug.Log($"[BootCheck][START] {DisplayName} Required:{required}");

        yield return RunCheck();

        IsCompleted = true;

        string level = IsSuccess ? "OK" : (required ? "FAILED" : "WARNING");
        Debug.Log($"[BootCheck][{level}] {DisplayName} / {Message}");
    }

    protected abstract IEnumerator RunCheck();

    protected void Pass(string message)
    {
        IsSuccess = true;
        Message = message;
    }

    protected void Fail(string message)
    {
        IsSuccess = false;
        Message = message;
    }
}
