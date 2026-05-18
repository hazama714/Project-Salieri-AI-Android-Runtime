using UnityEngine;
using SalieriAI.Core.Limbo;

public class NeuralEngineManager : MonoBehaviour
{
    [Header("Mode")]
    public ControlModeManager modeManager;

    [Header("Body Output")]
    public NeckController neckController;

    [Header("Limbo")]
    [SerializeField] private LimboPermission limboPermission;

    [Header("Soft Resume")]
    [SerializeField] private float softResumeSeconds = 0.8f;

    private float yawAngle = 90f;
    private float pitchAngle = 90f;

    private float outputYawAngle = 90f;
    private float outputPitchAngle = 90f;

    private float lastYawAngle = 999f;
    private float lastPitchAngle = 999f;

    private bool softResumeActive = false;
    private float softResumeTimer = 0f;
    private float softResumeStartYaw = 90f;
    private float softResumeStartPitch = 90f;

    private bool warnedModeManagerMissing = false;
    private bool warnedNeckControllerMissing = false;
    private bool wasBlockedByLimbo = false;

    // Limbo解除直後に古い角度を即送信しないためのフラグ
    private bool needResetAfterResume = false;

    private void Update()
    {
        if (modeManager == null)
        {
            if (!warnedModeManagerMissing)
            {
                Debug.LogWarning("[NeuralEngine] modeManager is not set.");
                warnedModeManagerMissing = true;
            }

            return;
        }

        if (!modeManager.IsNormalMode())
            return;

        if (neckController == null)
        {
            if (!warnedNeckControllerMissing)
            {
                Debug.LogWarning("[NeuralEngine] neckController is not set.");
                warnedNeckControllerMissing = true;
            }

            return;
        }

        if (!CanOutputToBody())
            return;

        bool resumedFromLimboBlock = wasBlockedByLimbo;

        wasBlockedByLimbo = false;

        if (resumedFromLimboBlock && needResetAfterResume)
        {
            needResetAfterResume = false;

            outputYawAngle = yawAngle;
            outputPitchAngle = pitchAngle;

            lastYawAngle = outputYawAngle;
            lastPitchAngle = outputPitchAngle;

            softResumeActive = false;
            softResumeTimer = 0f;

            Debug.Log(
                $"[NeuralEngine] Resume reset Yaw:{outputYawAngle} Pitch:{outputPitchAngle}"
            );

            return;
        }

        UpdateOutputAngles();

        bool changed =
            Mathf.Abs(outputYawAngle - lastYawAngle) > 0.01f ||
            Mathf.Abs(outputPitchAngle - lastPitchAngle) > 0.01f;

        if (!changed)
            return;

        lastYawAngle = outputYawAngle;
        lastPitchAngle = outputPitchAngle;

        Debug.Log($"[NeuralEngine] LookAt Yaw:{outputYawAngle} Pitch:{outputPitchAngle}");

        neckController.LookAt(outputYawAngle, outputPitchAngle);
    }

    private bool CanOutputToBody()
    {
        if (limboPermission == null)
            return true;

        if (limboPermission.IsEmergencyMode)
        {
            needResetAfterResume = true;
            LogBlockedOnce("[NeuralEngine] Blocked by Limbo: Emergency");
            return false;
        }

        if (!limboPermission.CanTrackFace)
        {
            needResetAfterResume = true;
            LogBlockedOnce("[NeuralEngine] Blocked by Limbo: CanTrackFace=false");
            return false;
        }

        if (!limboPermission.CanMoveServo)
        {
            needResetAfterResume = true;
            LogBlockedOnce("[NeuralEngine] Blocked by Limbo: CanMoveServo=false");
            return false;
        }

        return true;
    }

    private void LogBlockedOnce(string message)
    {
        if (wasBlockedByLimbo)
            return;

        Debug.Log(message);
        wasBlockedByLimbo = true;
    }

    private void UpdateOutputAngles()
    {
        if (!softResumeActive)
        {
            outputYawAngle = yawAngle;
            outputPitchAngle = pitchAngle;
            return;
        }

        softResumeTimer += Time.deltaTime;

        float t = softResumeSeconds <= 0f
            ? 1f
            : Mathf.Clamp01(softResumeTimer / softResumeSeconds);

        t = Mathf.SmoothStep(0f, 1f, t);

        outputYawAngle = Mathf.Lerp(softResumeStartYaw, yawAngle, t);
        outputPitchAngle = Mathf.Lerp(softResumeStartPitch, pitchAngle, t);

        if (t >= 1f)
            softResumeActive = false;
    }

    public void BeginSoftResume(float fromYaw, float fromPitch)
    {
        softResumeStartYaw = fromYaw;
        softResumeStartPitch = fromPitch;

        outputYawAngle = fromYaw;
        outputPitchAngle = fromPitch;

        lastYawAngle = 999f;
        lastPitchAngle = 999f;

        softResumeTimer = 0f;
        softResumeActive = true;

        needResetAfterResume = false;
        wasBlockedByLimbo = false;

        Debug.Log($"[NeuralEngine] SoftResume from Yaw:{fromYaw} Pitch:{fromPitch}");
    }

    public void SetYaw(float value)
    {
        yawAngle = value;
    }

    public void SetPitch(float value)
    {
        pitchAngle = value;
    }

    public void SetLookAt(float yaw, float pitch)
    {
        yawAngle = yaw;
        pitchAngle = pitch;
    }
}