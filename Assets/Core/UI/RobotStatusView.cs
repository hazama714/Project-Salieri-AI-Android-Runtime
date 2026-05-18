using UnityEngine;
using UnityEngine.UI;
using SalieriAI.Autonomy;

public sealed class RobotStatusView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text statusText;
    [SerializeField] private Text speechText;

    [Header("Refs")]
    [SerializeField] private SelfStateCollector selfStateCollector;

    [Header("Display")]
    [SerializeField] private string emptySpeechText = "";

    private string lastSpeech = "";

    private void Reset()
    {
        statusText = GetComponent<Text>();
    }

    private void OnEnable()
    {
        ResponseBus.OnResponse += OnSpeechReceived;
    }

    private void OnDisable()
    {
        ResponseBus.OnResponse -= OnSpeechReceived;
    }

    private void Update()
    {
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (statusText == null || selfStateCollector == null)
            return;

        SelfStateSnapshot state = selfStateCollector.Collect();

        string displayState;

        if (state.faceDetected)
        {
            displayState = state.lastAction == "speak"
                ? "会話中"
                : "見つめています";
        }
        else
        {
            if (state.lastAction == "lookAround")
            {
                displayState = "人を探しています";
            }
            else if (state.lastAction == "speak")
            {
                displayState = "誰かを待っています";
            }
            else
            {
                displayState = "静かに待機しています";
            }
        }

        statusText.text = "状態 : " + displayState;
    }

    private void OnSpeechReceived(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        lastSpeech = text.Trim();

        if (speechText != null)
        {
            speechText.text = "発話 : " + lastSpeech;
        }
    }

    public void ClearSpeech()
    {
        lastSpeech = "";

        if (speechText != null)
        {
            speechText.text = emptySpeechText;
        }
    }
}