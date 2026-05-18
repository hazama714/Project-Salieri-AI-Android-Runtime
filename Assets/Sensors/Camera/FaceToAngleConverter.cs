using UnityEngine;

public class FaceToAngleConverter : MonoBehaviour
{
    [Header("Input")]
    public FaceDetector_OpenCV detector;

    [Header("Output")]
    public NeuralEngineManager engine;

    [Header("Range")]
    public float yawRange = 60f;
    public float pitchRange = 30f;

    [Header("Direction")]
    public bool invertYaw = true;
    public bool invertPitch = false;

    [Header("Startup Guard")]
    public float startupDelay = 1.5f;

    [Header("Face Stability")]
    public float requiredFaceStableTime = 0.25f;

    [Header("Smoothing")]
    public float smoothSpeed = 4f;

    [Header("Update Control")]
    public float updateInterval = 0.1f;
    public float minAngleDelta = 2f;

    [Header("Safety")]
    public float deadZone = 0.05f;
    public float maxStepPerUpdate = 5f;

    [Header("Debug")]
    public bool enableDebugLog = true;

    private float currentYaw = 0f;
    private float currentPitch = 0f;

    private float lastSentYaw = 0f;
    private float lastSentPitch = 0f;

    private float updateTimer;
    private float startTime;
    private float faceStableTimer;

    private bool isTracking;

    private void Start()
    {
        startTime = Time.time;

        currentYaw = 0f;
        currentPitch = 0f;

        lastSentYaw = 0f;
        lastSentPitch = 0f;

        updateTimer = 0f;
        faceStableTimer = 0f;
        isTracking = false;
    }

    private void Update()
    {
        if (detector == null || engine == null)
            return;

        if (Time.time - startTime < startupDelay)
            return;

        if (!detector.HasFace)
        {
            updateTimer = 0f;
            faceStableTimer = 0f;
            isTracking = false;
            return;
        }

        faceStableTimer += Time.deltaTime;

        if (!isTracking)
        {
            if (faceStableTimer < requiredFaceStableTime)
                return;

            isTracking = true;

            if (enableDebugLog)
                Debug.Log("[FaceToAngleConverter] Tracking started");
        }

        updateTimer += Time.deltaTime;

        if (updateTimer < updateInterval)
            return;

        updateTimer = 0f;

        float width = detector.FrameWidth;
        float height = detector.FrameHeight;

        if (width <= 0f || height <= 0f)
            return;

        float centerX = detector.FaceCenterX;
        float centerY = detector.FaceCenterY;

        float offsetX = (centerX - width * 0.5f) / (width * 0.5f);
        float offsetY = (centerY - height * 0.5f) / (height * 0.5f);

        offsetX = Mathf.Clamp(offsetX, -1f, 1f);
        offsetY = Mathf.Clamp(offsetY, -1f, 1f);

        if (Mathf.Abs(offsetX) < deadZone)
            offsetX = 0f;

        if (Mathf.Abs(offsetY) < deadZone)
            offsetY = 0f;

        float targetYaw = offsetX * yawRange;
        float targetPitch = -offsetY * pitchRange;

        if (invertYaw)
            targetYaw *= -1f;

        if (invertPitch)
            targetPitch *= -1f;

        targetYaw = Mathf.Clamp(targetYaw, -yawRange, yawRange);
        targetPitch = Mathf.Clamp(targetPitch, -pitchRange, pitchRange);

        float smoothedYaw = Mathf.Lerp(
            currentYaw,
            targetYaw,
            Time.deltaTime * smoothSpeed
        );

        float smoothedPitch = Mathf.Lerp(
            currentPitch,
            targetPitch,
            Time.deltaTime * smoothSpeed
        );

        currentYaw = Mathf.MoveTowards(
            currentYaw,
            smoothedYaw,
            maxStepPerUpdate
        );

        currentPitch = Mathf.MoveTowards(
            currentPitch,
            smoothedPitch,
            maxStepPerUpdate
        );

        bool changed =
            Mathf.Abs(currentYaw - lastSentYaw) >= minAngleDelta ||
            Mathf.Abs(currentPitch - lastSentPitch) >= minAngleDelta;

        if (!changed)
            return;

        lastSentYaw = currentYaw;
        lastSentPitch = currentPitch;

        if (enableDebugLog)
        {
            Debug.Log(
                $"[FaceToAngleConverter] " +
                $"Center:{centerX:F1},{centerY:F1} " +
                $"Frame:{width:F0}x{height:F0} " +
                $"Stable:{faceStableTimer:F2} " +
                $"Offset:{offsetX:F2},{offsetY:F2} " +
                $"Target:{targetYaw:F1},{targetPitch:F1} " +
                $"Send:{currentYaw:F1},{currentPitch:F1}"
            );
        }

        engine.SetLookAt(currentYaw, currentPitch);
    }
}