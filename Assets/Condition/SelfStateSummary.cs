using System;
using UnityEngine;

[Serializable]
public class SelfStateSummary
{
    // ============================================================
    // SelfStateSummary
    // ------------------------------------------------------------
    // RobotConditionCollector が収集した「今の自分の状態」を保持する。
    //
    // 重要：
    // ここは判断しない。
    // ここは行動を決めない。
    // ここは状態値を渡すだけ。
    //
    // Cloud / Local / Limbo / Autonomy が参照する共通の状態DTO。
    // ============================================================

    [Header("Device / Boot")]
    // 主にCloud/Local共通。
    // 起動・使用可能デバイスの状態。
    public bool cameraAvailable;
    public bool microphoneAvailable;
    public bool bluetoothReady;
    public bool llamaReady;
    public bool voicevoxReady;

    // 現時点では将来用寄り。
    // センサーセルフチェックや環境反応に使える。
    public bool lightSensorAvailable;
    public bool gyroAvailable;

    [Header("Power / Thermal")]
    // Cloud/Local共通。
    // 将来的には省電力・縮退運転・Local停止判断に使う。
    public float batteryLevel;
    public BatteryStatus batteryStatus;
    public float maxTemperatureCelsius;

    [Header("Interaction State")]
    // Limbo / Cloud / Local 共通。
    // 今の状態名。例: Idle, Thinking, Speaking, Acting, Booting。
    public string interactionState = "Idle";

    // 互換用・将来整理候補。
    // 現状は interactionState と同じ値が入ることが多い。
    public string mode = "idle";

    [Header("Perception")]
    // Cloud/Local共通。
    // 現在この瞬間に顔が検出されているか。
    public bool faceDetected;

    // Cloud/Local共通。
    // 最後に顔を検出してからの経過秒。
    public float secondsSinceLastFace;

    // 主にCloud用。
    // 顔がStableFoundとして継続して見えている時間。
    // 「ずっと見られている」「そばにいる」判断材料。
    public float faceVisibleDuration;

    // 主にCloud用。
    // TemporaryLost / FullyLost など、顔が安定して見えていない継続時間。
    // 「しばらく見えない」「探す/呼ぶ」判断材料。
    public float noFaceDuration;

    [Header("Activity")]
    // Limbo / Cloud / Local 共通。
    // 発話中・行動中かどうか。
    public bool isSpeaking;
    public bool isActing;

    // Cloud/Local共通。
    // 思考、行動、発話からの経過時間。
    public float secondsSinceLastThink;
    public float secondsSinceLastAction;
    public float secondsSinceLastSpeech;

    // Cloud/Local共通。
    // 直前の行動ID。例: none, lookAround, speak。
    public string lastAction = "none";

    [Header("Limbo Permission")]
    // Limbo状態の参照値。
    // 原則としてLLMが直接変更しない。
    // Promptでは「今できること」の説明材料として使う。
    public bool canThink;
    public bool canSpeak;
    public bool canMoveServo;
    public bool canStartAction;
    public bool canTrackFace;
    public bool canInterrupt;

    [Header("Memo")]
    // 将来用。
    // デバッグメモ、状態要約、外部入力の一時注釈などに使える。
    [TextArea(2, 5)]
    public string note;

    public string ToPromptJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public string ToShortPromptText()
    {
        return
            $"state={interactionState}, " +
            $"face={faceDetected}, " +
            $"lastFace={secondsSinceLastFace:F1}s, " +
            $"faceVisible={faceVisibleDuration:F1}s, " +
            $"noFace={noFaceDuration:F1}s, " +
            $"lastAction={secondsSinceLastAction:F1}s, " +
            $"lastSpeech={secondsSinceLastSpeech:F1}s, " +
            $"speaking={isSpeaking}, " +
            $"acting={isActing}, " +
            $"canThink={canThink}, " +
            $"canMove={canMoveServo}, " +
            $"canSpeak={canSpeak}, " +
            $"battery={batteryLevel:F2}, " +
            $"temp={maxTemperatureCelsius:F1}, " +
            $"lastActionId={lastAction}";
    }
}