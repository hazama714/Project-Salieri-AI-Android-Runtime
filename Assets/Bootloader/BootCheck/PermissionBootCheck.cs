using System.Collections;
using UnityEngine;

public class PermissionBootCheck : BootCheckBase
{
    [Header("Permissions")]
    [SerializeField] private bool checkCamera = true;
    [SerializeField] private bool checkMicrophone = true;

    protected override IEnumerator RunCheck()
    {
        if (checkCamera && !Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Fail("Camera permission is not granted.");
            yield break;
        }

        if (checkMicrophone && !Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Fail("Microphone permission is not granted.");
            yield break;
        }

        Pass($"Permissions OK. Camera:{checkCamera} Microphone:{checkMicrophone}");
        yield return null;
    }
}
