using System.Collections;
using UnityEngine;

public class BatteryBootCheck : BootCheckBase
{
    [Header("Battery")]
    [SerializeField] private float warningLevel = 0.20f;
    [SerializeField] private float criticalLevel = 0.10f;

    protected override IEnumerator RunCheck()
    {
        float level = SystemInfo.batteryLevel;
        BatteryStatus status = SystemInfo.batteryStatus;

        if (level < 0f)
        {
            Fail("Battery level is unknown. Status: " + status);
            yield break;
        }

        Debug.Log($"[BatteryBootCheck] Level:{level:0.00} Status:{status}");

        if (level <= criticalLevel && status != BatteryStatus.Charging)
        {
            Fail($"Battery critical: {level:P0} Status:{status}");
            yield break;
        }

        if (level <= warningLevel && status != BatteryStatus.Charging)
        {
            Fail($"Battery low warning: {level:P0} Status:{status}");
            yield break;
        }

        Pass($"Battery OK: {level:P0} Status:{status}");
        yield return null;
    }
}
