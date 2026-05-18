/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */

// RuntimeConnectionSettings support

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using SalieriAI.Core.Limbo;
using SalieriAI.Core.State;
using SalieriAI.Runtime;

public sealed class VoicePlaybackController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VoicevoxAndroidBridge voicevoxBridge;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private LimboPermission limboPermission;
    [SerializeField] private InteractionStateController stateController;

    [Header("Runtime")]
    [SerializeField] private RuntimeConnectionSettings runtimeSettings;

    [Header("VOICEVOX Settings")]
    [SerializeField] private int styleId = 14;
    [SerializeField] private string outputFileName = "voicevox_last.wav";

    [Header("Debug")]
    [SerializeField] private bool subscribeOnEnable = true;
    [SerializeField] private bool logVerbose = true;

    [Header("Debug Test")]
    [SerializeField] private bool playOnStartForDebug = false;

    [SerializeField]
    [TextArea]
    private string debugText = "Ç±ÇÒÇ…ÇøÇÕÅB";

    private Coroutine playbackCoroutine;
    private InteractionState stateBeforeSpeaking = InteractionState.Idle;

    private void Awake()
    {
        if (voicevoxBridge == null)
            voicevoxBridge = FindObjectOfType<VoicevoxAndroidBridge>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (limboPermission == null)
            limboPermission = FindObjectOfType<LimboPermission>();

        if (stateController == null)
            stateController = FindObjectOfType<InteractionStateController>();

        if (runtimeSettings == null)
            runtimeSettings = FindObjectOfType<RuntimeConnectionSettings>();

        if (logVerbose)
        {
            Debug.Log(
                $"[VoicePlaybackController][Awake] " +
                $"bridge={(voicevoxBridge != null ? voicevoxBridge.name : "null")} " +
                $"audioSource={(audioSource != null ? audioSource.name : "null")} " +
                $"limbo={(limboPermission != null ? limboPermission.name : "null")} " +
                $"state={(stateController != null ? stateController.name : "null")} " +
                $"runtime={(runtimeSettings != null ? runtimeSettings.name : "null")} " +
                $"outputFileName={outputFileName}"
            );
        }
    }

    private void Start()
    {
        if (playOnStartForDebug)
            PlayText(debugText);
    }

    private void OnEnable()
    {
        if (!subscribeOnEnable)
            return;

        ResponseBus.OnResponse += OnResponse;

        if (logVerbose)
            Debug.Log("[VoicePlaybackController] subscribed ResponseBus.OnResponse");
    }

    private void OnDisable()
    {
        if (!subscribeOnEnable)
            return;

        ResponseBus.OnResponse -= OnResponse;

        if (logVerbose)
            Debug.Log("[VoicePlaybackController] unsubscribed ResponseBus.OnResponse");
    }

    private void OnResponse(string text)
    {
        PlayText(text);
    }

    public void PlayText(string text)
    {
        if (!IsVoiceEnabled())
        {
            Debug.Log("[VoicePlaybackController] Voice disabled by RuntimeConnectionSettings.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            Debug.LogWarning("[VoicePlaybackController] text is empty");
            return;
        }

        if (!CanSpeakNow())
        {
            Debug.Log("[VoicePlaybackController] Speak blocked by Limbo.");
            return;
        }

        if (playbackCoroutine != null)
            StopCoroutine(playbackCoroutine);

        playbackCoroutine = StartCoroutine(PlayTextRoutine(text.Trim()));
    }

    private IEnumerator PlayTextRoutine(string text)
    {
        if (!IsVoiceEnabled())
        {
            Debug.Log("[VoicePlaybackController] Voice disabled at routine start.");
            playbackCoroutine = null;
            yield break;
        }

        if (voicevoxBridge == null)
        {
            Debug.LogError("[VoicePlaybackController] voicevoxBridge is null");
            playbackCoroutine = null;
            yield break;
        }

        if (audioSource == null)
        {
            Debug.LogError("[VoicePlaybackController] audioSource is null");
            playbackCoroutine = null;
            yield break;
        }

        if (!CanSpeakNow())
        {
            Debug.Log("[VoicePlaybackController] Speak blocked by Limbo at routine start.");
            playbackCoroutine = null;
            yield break;
        }

        EnterSpeakingState();

        if (logVerbose)
            Debug.Log($"[VoicePlaybackController] Synthesize: {text}");

        string wavPath;

        try
        {
            wavPath = voicevoxBridge.SynthesizeToFile(
                text,
                styleId,
                outputFileName
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoicePlaybackController] SynthesizeToFile failed: {e}");
            ExitSpeakingState();
            playbackCoroutine = null;
            yield break;
        }

        if (string.IsNullOrEmpty(wavPath))
        {
            Debug.LogError("[VoicePlaybackController] SynthesizeToFile returned empty path");
            ExitSpeakingState();
            playbackCoroutine = null;
            yield break;
        }

        if (!File.Exists(wavPath))
        {
            Debug.LogError($"[VoicePlaybackController] wav not found: {wavPath}");
            ExitSpeakingState();
            playbackCoroutine = null;
            yield break;
        }

        string url = "file://" + wavPath;

        using (UnityWebRequest request =
               UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[VoicePlaybackController] Load wav failed: {request.error}");
                ExitSpeakingState();
                playbackCoroutine = null;
                yield break;
            }

            if (!IsVoiceEnabled())
            {
                Debug.Log("[VoicePlaybackController] Voice disabled before playback.");
                ExitSpeakingState();
                playbackCoroutine = null;
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            if (logVerbose)
            {
                Debug.Log(
                    $"[VoicePlaybackController] AudioSource.Play " +
                    $"path:{wavPath} length:{clip.length:F2}"
                );
            }

            while (audioSource != null && audioSource.isPlaying)
            {
                if (!IsVoiceEnabled())
                {
                    audioSource.Stop();
                    Debug.Log("[VoicePlaybackController] Playback stopped by RuntimeConnectionSettings.");
                    break;
                }

                yield return null;
            }
        }

        ExitSpeakingState();
        playbackCoroutine = null;
    }

    private bool IsVoiceEnabled()
    {
        if (runtimeSettings == null)
            return true;

        return runtimeSettings.useVoice;
    }

    private bool CanSpeakNow()
    {
        if (limboPermission == null)
            return true;

        if (limboPermission.IsEmergencyMode)
            return false;

        return limboPermission.CanSpeak;
    }

    private void EnterSpeakingState()
    {
        if (stateController == null)
            return;

        stateBeforeSpeaking = stateController.CurrentState;
        stateController.SetSpeaking();
    }

    private void ExitSpeakingState()
    {
        if (stateController == null)
            return;

        if (stateBeforeSpeaking == InteractionState.Tracking ||
            stateBeforeSpeaking == InteractionState.TemporaryLost ||
            stateBeforeSpeaking == InteractionState.FullyLost)
        {
            stateController.SetState(stateBeforeSpeaking);
            return;
        }

        stateController.SetIdle();
    }
}