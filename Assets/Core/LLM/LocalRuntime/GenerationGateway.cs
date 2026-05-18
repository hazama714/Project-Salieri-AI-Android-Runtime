// ============================================================
// File: GenerationGateway.cs
// Version: Salieri.GenerationGateway v1.3.0
// Purpose:
// - LLM�����v���̌�ʐ���
// - SingleFlight�ɂ�鑽�d�����h�~
// - Profile�Ⴂ��SingleFlight key�֔��f
// ============================================================

using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Salieri.Infra;
using UnityEngine;

namespace Salieri.Runtime
{
    public sealed class GenerationGateway
    {
        private const string TAG = "[GenerationGateway]";

        private readonly SingleFlight _singleFlight = new SingleFlight();
        private readonly Func<IntPtr> _ctxProvider;
        private readonly Func<string, Task<string>> _nativeGenerateAsync;

        public GenerationGateway(
            Func<IntPtr> ctxProvider,
            Func<string, Task<string>> nativeGenerateAsync)
        {
            _ctxProvider = ctxProvider;
            _nativeGenerateAsync = nativeGenerateAsync;

            ResumeHook.CtxProvider = _ctxProvider;
            // Local LLM warmup can be expensive on Android.
            // Keep context provider for resume checks, but do not auto-generate <ping/> on resume.
            ResumeHook.WarmupProvider = null;

            Debug.Log($"{TAG} created");
        }

        public Task<string> GenerateAsync(string prompt)
        {
            return GenerateAsync(prompt, "default");
        }

        public Task<string> GenerateAsync(string prompt, string profileKey)
        {
            string safePrompt = prompt ?? string.Empty;
            string safeProfileKey = string.IsNullOrEmpty(profileKey)
                ? "default"
                : profileKey;

            string key = "gen:" + safeProfileKey + ":" + Sha1(safePrompt);

            Debug.Log(
                $"{TAG} GenerateAsync ENTER " +
                $"profile={safeProfileKey} key={key} promptLen={safePrompt.Length}"
            );

            return _singleFlight.DoAsync(key, async () =>
            {
                Debug.Log($"{TAG} SingleFlight ENTER key={key}");

                if (_nativeGenerateAsync == null)
                {
                    Debug.LogError($"{TAG} nativeGenerateAsync is null");
                    return string.Empty;
                }

                IntPtr ctx = IntPtr.Zero;

                try
                {
                    if (_ctxProvider != null)
                        ctx = _ctxProvider();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{TAG} ctxProvider exception: {ex.GetType().Name} {ex.Message}");
                }

                Debug.Log($"{TAG} ctx={ctx}");

                if (ctx == IntPtr.Zero)
                {
                    Debug.LogWarning($"{TAG} ctx is zero, but continue for generation debug");
                }

                string result = string.Empty;

                try
                {
                    Debug.Log($"{TAG} BEFORE nativeGenerateAsync");

                    result = await _nativeGenerateAsync(safePrompt);

                    Debug.Log($"{TAG} AFTER nativeGenerateAsync len={(result ?? string.Empty).Length}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{TAG} nativeGenerateAsync exception: {ex.GetType().Name} {ex.Message}");
                    Debug.LogException(ex);
                    result = string.Empty;
                }

                Debug.Log($"{TAG} GenerateAsync DONE len={(result ?? string.Empty).Length}");

                return result ?? string.Empty;
            });
        }

        public Task WarmupAsync(string pingPrompt = "<ping/>")
        {
            Debug.Log($"{TAG} WarmupAsync ENTER");

            return _singleFlight.DoAsync("warmup", async () =>
            {
                if (_nativeGenerateAsync == null)
                {
                    Debug.LogWarning($"{TAG} warmup skipped: nativeGenerateAsync is null");
                    return false;
                }

                try
                {
                    IntPtr ctx = IntPtr.Zero;

                    if (_ctxProvider != null)
                        ctx = _ctxProvider();

                    Debug.Log($"{TAG} warmup ctx={ctx}");

                    if (ctx == IntPtr.Zero)
                    {
                        Debug.LogWarning($"{TAG} warmup skipped: ctx is zero");
                        return false;
                    }

                    string result = await _nativeGenerateAsync(pingPrompt ?? "<ping/>");

                    Debug.Log($"{TAG} warmup done len={(result ?? string.Empty).Length}");

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{TAG} warmup failed: {ex.GetType().Name} {ex.Message}");
                    return false;
                }
            });
        }

        private static string Sha1(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "0";

            using (var sha1 = SHA1.Create())
            {
                byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(bytes.Length * 2);

                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }
    }
}