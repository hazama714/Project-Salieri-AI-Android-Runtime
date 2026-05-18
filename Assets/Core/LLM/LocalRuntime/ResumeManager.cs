/*
 * Project Salieri AI
 * Copyright (c) 2026 Hazama Kaizuka
 *
 * Licensed under the Apache License, Version 2.0
 * http://www.apache.org/licenses/LICENSE-2.0
 */
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Salieri.Runtime
{
    public static class ResumeManager
    {
        // ---- Native shim interop（C++側で実装済みのAPI）----
        [DllImport("llama_unity_shim")] private static extern void unity_llama_reset_ctx_pos(IntPtr ctx);
        [DllImport("llama_unity_shim")] private static extern void unity_llama_hard_reset(IntPtr ctx);

        private static readonly object _gate = new();
        private static Task _inflight;
        private static int _resumeEpoch; // 何回復帰したか（世代ID）

        /// <summary> 復帰直後の1回だけ：生成前に呼ぶ（既存API・互換維持）。 </summary>
        public static Task EnsureResumedAsync(Func<IntPtr> ctxProvider, int backoffMs = 120)
        {
            lock (_gate)
            {
                if (_inflight != null && !_inflight.IsCompleted)
                    return _inflight;

                _inflight = Task.Run(async () =>
                {
                    try
                    {
                        IntPtr ctx = IntPtr.Zero;
                        if (ctxProvider != null)
                            ctx = ctxProvider();

                        if (ctx != IntPtr.Zero)
                        {
                            // ハードリセット（KVクリア＋pos=0）
                            unity_llama_hard_reset(ctx);
                        }

                        // 復帰直後はスケジューラが揺れるので短い待ちを一度だけ
                        await Task.Delay(Mathf.Clamp(backoffMs, 50, 250)).ConfigureAwait(false);
                        Interlocked.Increment(ref _resumeEpoch);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[ResumeManager] handshake skipped: {e.Message}");
                    }
                });
                return _inflight;
            }
        }

        /// <summary>
        /// 復帰ハンドシェイク（EnsureResumed）＋ 任意のウォームアップ処理。
        /// ウォームアップは null ならスキップ。
        /// </summary>
        public static async Task ResumeHandshakeAsync(Func<IntPtr> ctxProvider, Func<Task> warmup = null, int backoffMs = 120)
        {
            await EnsureResumedAsync(ctxProvider, backoffMs).ConfigureAwait(false);

            if (warmup != null)
            {
                try
                {
                    await warmup().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ResumeManager] warmup skipped: {e.Message}");
                }
            }
        }

        /// <summary> 世代ID（resumeごとに+1）。古い生成の無効化などに利用。 </summary>
        public static int CurrentEpoch => Volatile.Read(ref _resumeEpoch);
    }

    /// <summary>
    /// Unity ライフサイクルで自動トリガする薄いMonoBehaviour（既存のInit順序には干渉しない）。
    /// </summary>
    public sealed class ResumeHook : MonoBehaviour
    {
        // ランタイムが保持している ctx を取るための委譲。起動時に外部から設定する。
        public static Func<IntPtr> CtxProvider;

        // 🔸 GenerationGateway 側からセットされる任意のウォームアップ処理
        public static Func<Task> WarmupProvider;

        private bool _paused;

        private async void OnApplicationPause(bool pause)
        {
            _paused = pause;
            if (!pause) // 復帰
            {
                await ResumeManager.ResumeHandshakeAsync(
                    ctxProvider: () => CtxProvider != null ? CtxProvider() : IntPtr.Zero,
                    warmup: WarmupProvider
                );
            }
        }

        private async void OnApplicationFocus(bool focus)
        {
            if (focus && _paused == false) // バックグラウンドからのフォーカス復帰も拾う
            {
                await ResumeManager.ResumeHandshakeAsync(
                    ctxProvider: () => CtxProvider != null ? CtxProvider() : IntPtr.Zero,
                    warmup: WarmupProvider
                );
            }
        }
    }
}
