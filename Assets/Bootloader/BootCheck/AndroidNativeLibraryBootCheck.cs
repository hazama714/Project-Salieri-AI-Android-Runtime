using System;
using System.Collections;
using UnityEngine;

public class AndroidNativeLibraryBootCheck : BootCheckBase
{
    [Header("Native Library")]
    [SerializeField] private string libraryNameWithoutPrefix = "llama_unity_shim";

    protected override IEnumerator RunCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(libraryNameWithoutPrefix))
        {
            Fail("library name is empty.");
            yield break;
        }

        try
        {
            using (AndroidJavaClass system = new AndroidJavaClass("java.lang.System"))
            {
                system.CallStatic("loadLibrary", libraryNameWithoutPrefix);
            }

            Pass("Loaded: lib" + libraryNameWithoutPrefix + ".so");
        }
        catch (Exception ex)
        {
            Fail("Load failed: lib" + libraryNameWithoutPrefix + ".so / " + ex.Message);
        }
#else
        Pass("Skipped native load outside Android runtime: " + libraryNameWithoutPrefix);
#endif
        yield return null;
    }
}
