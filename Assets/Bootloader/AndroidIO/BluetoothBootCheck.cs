using System.Collections;
using UnityEngine;

public class BluetoothBootCheck : BootCheckBase
{
    [Header("Bluetooth")]
    [SerializeField] private bool requireEnabled = true;

    protected override IEnumerator RunCheck()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass adapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
            using (AndroidJavaObject adapter = adapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter"))
            {
                if (adapter == null)
                {
                    Fail("Bluetooth adapter is not available.");
                    yield break;
                }

                bool enabled = adapter.Call<bool>("isEnabled");

                if (requireEnabled && !enabled)
                {
                    Fail("Bluetooth adapter is disabled.");
                    yield break;
                }

                Pass("Bluetooth adapter available. Enabled:" + enabled);
            }
        }
        catch (System.Exception ex)
        {
            Fail("Bluetooth check failed: " + ex.Message);
        }
#else
        Pass("Skipped Bluetooth check outside Android runtime.");
#endif
        yield return null;
    }
}
