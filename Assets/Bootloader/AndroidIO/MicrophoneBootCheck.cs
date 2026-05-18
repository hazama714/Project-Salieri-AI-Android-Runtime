using System.Collections;
using UnityEngine;

public class MicrophoneBootCheck : BootCheckBase
{
    [Header("Microphone")]
    [SerializeField] private bool requirePermission = true;

    protected override IEnumerator RunCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (requirePermission && !Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Fail("Microphone permission is not granted.");
            yield break;
        }
#endif

        string[] devices = Microphone.devices;

        if (devices == null || devices.Length == 0)
        {
            Fail("No microphone devices found.");
            yield break;
        }

        for (int i = 0; i < devices.Length; i++)
            Debug.Log($"[MicrophoneBootCheck] Device {i}: {devices[i]}");

        Pass("Microphone device count: " + devices.Length);
        yield return null;
    }
}
