using System;
using UnityEngine;

[Serializable]
public class SelfCheckSnapshot
{
    public bool cameraAvailable;
    public bool microphoneAvailable;
    public bool bluetoothReady;
    public bool llamaReady;
    public bool voicevoxReady;
    public bool lightSensorAvailable;
    public bool gyroAvailable;
    public float batteryLevel;
    public BatteryStatus batteryStatus;
    public float maxTemperatureCelsius;
    public string note;
}
