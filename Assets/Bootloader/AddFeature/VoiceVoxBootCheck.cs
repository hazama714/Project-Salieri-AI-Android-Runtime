using System.Collections;
using UnityEngine;

public class VoiceVoxBootCheck : BootCheckBase
{
    [Header("VOICEVOX Native Libraries")]
    [SerializeField] private string[] librariesWithoutPrefix =
    {
        "voicevox_core",
        "onnxruntime"
    };

    protected override IEnumerator RunCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (librariesWithoutPrefix == null || librariesWithoutPrefix.Length == 0)
        {
            Fail("No VOICEVOX native libraries are configured.");
            yield break;
        }

        try
        {
            using (AndroidJavaClass system = new AndroidJavaClass("java.lang.System"))
            {
                foreach (string lib in librariesWithoutPrefix)
                {
                    if (string.IsNullOrEmpty(lib))
                        continue;

                    system.CallStatic("loadLibrary", lib);
                    Debug.Log("[VoiceVoxBootCheck] Loaded: lib" + lib + ".so");
                }
            }

            Pass("VOICEVOX native libraries loaded.");
        }
        catch (System.Exception ex)
        {
            Fail("VOICEVOX native load failed: " + ex.Message);
        }
#else
        Pass("Skipped VOICEVOX native load outside Android runtime.");
#endif
        yield return null;
    }
}
