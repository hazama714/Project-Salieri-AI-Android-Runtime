using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class AndroidCameraPermission : MonoBehaviour
{
    public bool IsGranted { get; private set; }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log("[AndroidCameraPermission][REQUEST]");
            Permission.RequestUserPermission(Permission.Camera);
        }
        else
        {
            IsGranted = true;
            Debug.Log("[AndroidCameraPermission][GRANTED_ALREADY]");
        }
#else
        IsGranted = true;
        Debug.Log("[AndroidCameraPermission][EDITOR]");
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsGranted && Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            IsGranted = true;
            Debug.Log("[AndroidCameraPermission][GRANTED]");
        }
#endif
    }
}