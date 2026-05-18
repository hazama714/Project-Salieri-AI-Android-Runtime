/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

// v1.4 / RuntimeConnectionSettings support

using UnityEngine;
using System;
using System.Text;
using SalieriAI.Runtime;

public class AndroidBluetoothSender : MonoBehaviour, ICommandSender
{
    [Header("Runtime Settings")]
    [SerializeField] private RuntimeConnectionSettings runtimeSettings;

    [Header("Bluetooth Target")]
    public string deviceName = "JDY-31-SPP";
    public string sppUuid = "00001101-0000-1000-8000-00805F9B34FB";

    [Header("Connection")]
    public bool autoConnectOnStart = false;
    public bool autoReconnect = true;
    public float reconnectCooldownSeconds = 5.0f;

    [Header("Send Guard")]
    public float minSendInterval = 0.05f;

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject bluetoothAdapter;
    private AndroidJavaObject bluetoothSocket;
    private AndroidJavaObject outputStream;
#endif

    private bool isConnected = false;
    private bool isConnecting = false;

    private bool hasPendingCommand = false;
    private int pendingServoId = 0;
    private int pendingAngle = 90;

    private float lastSendTime = -999f;
    private float nextReconnectTime = 0f;

    void Awake()
    {
        if (runtimeSettings == null)
            runtimeSettings = FindObjectOfType<RuntimeConnectionSettings>();

        Debug.Log("[AndroidBluetoothSender][AWAKE]");

        if (!IsBluetoothEnabled())
        {
            Debug.Log("[AndroidBluetoothSender][DISABLED] Bluetooth disabled by RuntimeConnectionSettings.");
            hasPendingCommand = false;
        }
    }

    void Start()
    {
        if (!IsBluetoothEnabled())
        {
            Debug.Log("[AndroidBluetoothSender][START_SKIP] Bluetooth disabled.");
            return;
        }

        Debug.Log(
            $"[AndroidBluetoothSender][START] " +
            $"autoConnectOnStart:{autoConnectOnStart} " +
            $"autoReconnect:{autoReconnect} " +
            $"deviceName:{deviceName}"
        );

        if (autoConnectOnStart)
            RequestReconnectNow();
    }

    void Update()
    {
        if (!IsBluetoothEnabled())
            return;

        TryAutoReconnect();
        TrySendPendingCommand();
    }

    public void SendServo(int servoId, int angle)
    {
        if (!IsBluetoothEnabled())
        {
            Debug.Log($"[AndroidBluetoothSender][SEND_SKIP] Bluetooth disabled. id:{servoId} angle:{angle}");
            return;
        }

        Debug.Log($"[AndroidBluetoothSender][CALL] id:{servoId} angle:{angle}");

        pendingServoId = servoId;
        pendingAngle = angle;
        hasPendingCommand = true;

        TrySendPendingCommand();
    }

    public void RequestReconnectNow()
    {
        if (!IsBluetoothEnabled())
        {
            Debug.Log("[AndroidBluetoothSender][RECONNECT_SKIP] Bluetooth disabled.");
            return;
        }

        nextReconnectTime = 0f;
        TryAutoReconnect();
    }

    private bool IsBluetoothEnabled()
    {
        if (runtimeSettings == null)
            return true;

        return runtimeSettings.useBluetooth;
    }

    private void TryAutoReconnect()
    {
        if (!IsBluetoothEnabled())
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!autoReconnect)
            return;

        if (isConnected || isConnecting)
            return;

        if (Time.realtimeSinceStartup < nextReconnectTime)
            return;

        Debug.Log("[AndroidBluetoothSender][AUTO_RECONNECT]");
        Connect();
#endif
    }

    public void Connect()
    {
        if (!IsBluetoothEnabled())
        {
            Debug.Log("[AndroidBluetoothSender][CONNECT_SKIP] Bluetooth disabled.");
            return;
        }

        Debug.Log(
            $"[AndroidBluetoothSender][CONNECT_ENTER] " +
            $"isConnected:{isConnected} " +
            $"isConnecting:{isConnecting} " +
            $"deviceName:{deviceName}"
        );

#if UNITY_ANDROID && !UNITY_EDITOR
        if (isConnected)
        {
            Debug.Log("[AndroidBluetoothSender][CONNECT_SKIP] already connected");
            return;
        }

        if (isConnecting)
        {
            Debug.Log("[AndroidBluetoothSender][CONNECT_SKIP] already connecting");
            return;
        }

        isConnecting = true;
        Debug.Log("[AndroidBluetoothSender][CONNECT_BEGIN]");

        try
        {
            AndroidJavaClass bluetoothAdapterClass =
                new AndroidJavaClass("android.bluetooth.BluetoothAdapter");

            bluetoothAdapter =
                bluetoothAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter");

            if (bluetoothAdapter == null)
            {
                Debug.LogError("[AndroidBluetoothSender][ERROR] Bluetooth adapter is null");
                MarkConnectFailed();
                return;
            }

            bool enabled = bluetoothAdapter.Call<bool>("isEnabled");
            Debug.Log($"[AndroidBluetoothSender][ADAPTER] enabled:{enabled}");

            if (!enabled)
            {
                Debug.LogError("[AndroidBluetoothSender][ERROR] Bluetooth is disabled");
                MarkConnectFailed();
                return;
            }

            AndroidJavaObject bondedDevices =
                bluetoothAdapter.Call<AndroidJavaObject>("getBondedDevices");

            if (bondedDevices == null)
            {
                Debug.LogError("[AndroidBluetoothSender][ERROR] bondedDevices is null");
                MarkConnectFailed();
                return;
            }

            int bondedCount = bondedDevices.Call<int>("size");
            Debug.Log($"[AndroidBluetoothSender][BONDED_COUNT] {bondedCount}");

            AndroidJavaObject iterator =
                bondedDevices.Call<AndroidJavaObject>("iterator");

            AndroidJavaObject targetDevice = null;

            while (iterator.Call<bool>("hasNext"))
            {
                AndroidJavaObject device =
                    iterator.Call<AndroidJavaObject>("next");

                string name = device.Call<string>("getName");
                string address = device.Call<string>("getAddress");

                Debug.Log($"[AndroidBluetoothSender][FOUND] name:{name} address:{address}");

                if (name == deviceName)
                {
                    targetDevice = device;
                    Debug.Log($"[AndroidBluetoothSender][TARGET_MATCH] {name}");
                    break;
                }
            }

            if (targetDevice == null)
            {
                Debug.LogError($"[AndroidBluetoothSender][ERROR] Device not found: {deviceName}");
                MarkConnectFailed();
                return;
            }

            AndroidJavaClass uuidClass =
                new AndroidJavaClass("java.util.UUID");

            AndroidJavaObject uuid =
                uuidClass.CallStatic<AndroidJavaObject>("fromString", sppUuid);

            Debug.Log($"[AndroidBluetoothSender][SOCKET_CREATE] uuid:{sppUuid}");

            bluetoothSocket =
                targetDevice.Call<AndroidJavaObject>(
                    "createRfcommSocketToServiceRecord",
                    uuid
                );

            if (bluetoothSocket == null)
            {
                Debug.LogError("[AndroidBluetoothSender][ERROR] bluetoothSocket is null");
                MarkConnectFailed();
                return;
            }

            Debug.Log("[AndroidBluetoothSender][CANCEL_DISCOVERY]");
            bluetoothAdapter.Call<bool>("cancelDiscovery");

            Debug.Log("[AndroidBluetoothSender][SOCKET_CONNECT_BEFORE]");
            bluetoothSocket.Call("connect");
            Debug.Log("[AndroidBluetoothSender][SOCKET_CONNECT_AFTER]");

            outputStream =
                bluetoothSocket.Call<AndroidJavaObject>("getOutputStream");

            if (outputStream == null)
            {
                Debug.LogError("[AndroidBluetoothSender][ERROR] outputStream is null");
                MarkConnectFailed();
                Close();
                return;
            }

            isConnected = true;
            isConnecting = false;

            Debug.Log($"[AndroidBluetoothSender] Connected to {deviceName}");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[AndroidBluetoothSender][CONNECT_ERROR] " +
                $"{e.GetType().Name}: {e.Message}\n{e.StackTrace}"
            );

            MarkConnectFailed();
            Close();
        }
#else
        Debug.Log("[AndroidBluetoothSender][EDITOR_DUMMY] Connect skipped in Editor");
#endif
    }

    private void TrySendPendingCommand()
    {
        if (!IsBluetoothEnabled())
        {
            hasPendingCommand = false;
            return;
        }

        if (!hasPendingCommand)
            return;

        Debug.Log(
            $"[AndroidBluetoothSender][SEND_PENDING] " +
            $"isConnected:{isConnected} " +
            $"isConnecting:{isConnecting} " +
            $"deviceName:{deviceName} " +
            $"id:{pendingServoId} " +
            $"angle:{pendingAngle}"
        );

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isConnected)
        {
            Debug.LogWarning("[AndroidBluetoothSender][SEND_SKIP] not connected. Command dropped.");
            hasPendingCommand = false;
            return;
        }

        if (outputStream == null)
        {
            Debug.LogWarning("[AndroidBluetoothSender][SEND_SKIP] outputStream is null. Command dropped.");
            hasPendingCommand = false;
            MarkDisconnected();
            return;
        }

        if (Time.realtimeSinceStartup - lastSendTime < minSendInterval)
            return;

        string command = $"#{pendingServoId} P{pendingAngle}\n";

        Debug.Log($"[AndroidBluetoothSender][WRITE_BEFORE] [{command.Replace("\n", "\\n")}]");

        try
        {
            byte[] bytes = Encoding.ASCII.GetBytes(command);
            sbyte[] signedBytes = new sbyte[bytes.Length];

            for (int i = 0; i < bytes.Length; i++)
                signedBytes[i] = unchecked((sbyte)bytes[i]);

            outputStream.Call("write", signedBytes);
            outputStream.Call("flush");

            lastSendTime = Time.realtimeSinceStartup;
            hasPendingCommand = false;

            Debug.Log("[AndroidBluetoothSender][WRITE_AFTER]");
        }
        catch (Exception e)
        {
            Debug.LogError(
                $"[AndroidBluetoothSender][WRITE_ERROR] " +
                $"{e.GetType().Name}: {e.Message}\n{e.StackTrace}"
            );

            hasPendingCommand = false;
            MarkDisconnected();
            Close();
        }
#else
        if (Time.realtimeSinceStartup - lastSendTime < minSendInterval)
            return;

        Debug.Log($"[AndroidBluetoothSender][EDITOR_DUMMY] id:{pendingServoId} angle:{pendingAngle}");

        lastSendTime = Time.realtimeSinceStartup;
        hasPendingCommand = false;
#endif
    }

    private void MarkConnectFailed()
    {
        isConnected = false;
        isConnecting = false;
        nextReconnectTime = Time.realtimeSinceStartup + reconnectCooldownSeconds;

        Debug.Log(
            $"[AndroidBluetoothSender][CONNECT_FAILED] " +
            $"nextReconnectIn:{reconnectCooldownSeconds}s"
        );
    }

    private void MarkDisconnected()
    {
        isConnected = false;
        isConnecting = false;
        nextReconnectTime = Time.realtimeSinceStartup + reconnectCooldownSeconds;

        Debug.Log(
            $"[AndroidBluetoothSender][DISCONNECTED] " +
            $"nextReconnectIn:{reconnectCooldownSeconds}s"
        );
    }

    public void Close()
    {
        Debug.Log("[AndroidBluetoothSender][CLOSE]");

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (outputStream != null)
            {
                outputStream.Call("close");
                outputStream = null;
                Debug.Log("[AndroidBluetoothSender][CLOSE] outputStream closed");
            }

            if (bluetoothSocket != null)
            {
                bluetoothSocket.Call("close");
                bluetoothSocket = null;
                Debug.Log("[AndroidBluetoothSender][CLOSE] bluetoothSocket closed");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[AndroidBluetoothSender][CLOSE_ERROR] " +
                $"{e.GetType().Name}: {e.Message}"
            );
        }
#endif

        isConnected = false;
        isConnecting = false;
        hasPendingCommand = false;
    }

    void OnDestroy()
    {
        Close();
    }
}