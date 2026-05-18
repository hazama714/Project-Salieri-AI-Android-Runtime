// ============================================================
// File: LocalLLMRuntimeHost.cs
// Version: SALIERI-Host v2.4.0
// Update:
// - Profile付き生成 GenerateSafeAsync(prompt, profile, profileKey) を追加
// - Action / Speech で maxTokens / sampling を切替可能
// - n_ctx / n_threads は初期化固定のまま
// - 生成中のProfile競合を避けるため SemaphoreSlim で直列化
// ============================================================

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using Salieri.Runtime;
using SalieriAI.Core.LLM;

namespace SalieriAI.AndroidLLM
{
    [DisallowMultipleComponent]
    internal static class ShimNative
    {
        private const string Dll = "llama_unity_shim";

        [DllImport(Dll)] internal static extern void unity_llama_print_signature();
        [DllImport(Dll)] internal static extern void llama_unity_set_verify_required(bool on);

        [DllImport(Dll, CharSet = CharSet.Ansi)]
        internal static extern int llama_unity_verify_model(string sha256, string arch, string buildId);

        [DllImport(Dll)]
        internal static extern int llama_unity_dump_diagnostics(
            StringBuilder outBuf,
            int outCap,
            int maxLines
        );
    }

    public sealed class LocalLLMRuntimeHost : MonoBehaviour
    {
        public static LocalLLMRuntimeHost Instance { get; private set; }

        [Header("Model")]
        [Tooltip("Application.persistentDataPath 相対 or 絶対パス")]
        public string defaultModelPath = "models/phi-3-mini-4k-instruct-q4_k_m.gguf";

        [Tooltip("コンテキスト長")]
        public int nCtx = 1024;

        [Tooltip("推論スレッド数")]
        public int nThreads = 8;

        [Header("Runtime")]
        public bool autoInitOnAwake = true;

        [Header("Generation")]
        [Tooltip("1回の生成で予測する最大トークン数")]
        public int maxTokens = 96;

        [Header("Sampling (Minimal)")]
        public int top_k = 40;

        [Range(0f, 1f)]
        public float top_p = 0.95f;

        [Range(0f, 1f)]
        public float min_p = 0.05f;

        [Range(0.1f, 2.0f)]
        public float temperature = 0.8f;

        public int seed = -1;
        public int n_keep = 0;

        [Header("Sampling (Repetition)")]
        public float repeat_penalty = 1.25f;
        public int repeat_last_n = 128;
        public float presence_penalty = 0.00f;

        [Header("Stopping / Format")]
        public string stopCsv = "User:,Assistant:,</s>,<|eot_id|>";
        public bool strictJson = false;

        [Header("Chat Template")]
        public bool useModelTemplate = true;

        [TextArea(2, 6)]
        public string systemPrompt = "";

        public bool applySimpleChatTemplate = true;
        public string roleUser = "User";
        public string roleAssistant = "Assistant";

        [Header("Context Reset")]
        public bool resetCtxEachTurn = false;

        private static string cachedTemplatePrefix = null;

        private GenerationGateway _gateway;
        private readonly SemaphoreSlim generateLock = new SemaphoreSlim(1, 1);

        public bool IsReady => Runtime != null && Runtime.Initialized;
        public event Action<bool> OnReadyChanged;

        public LocalLLMRuntime Runtime { get; private set; }
        public string ModelInfoJson => Runtime?.ModelInfoJson;

        private void Awake()
        {
            try
            {
                ShimNative.llama_unity_set_verify_required(true);
            }
            catch
            {
                // ignore
            }

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LLM-Host] duplicate host detected. destroying this instance.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            try
            {
                ShimNative.unity_llama_print_signature();
            }
            catch
            {
                // ignore
            }

            if (transform.parent != null)
            {
                Debug.LogWarning("[LLM-Host] Host is child object. Detaching to root for DontDestroyOnLoad.");
                transform.SetParent(null, true);
            }

            DontDestroyOnLoad(gameObject);

            OnReadyChanged?.Invoke(false);

            if (autoInitOnAwake)
            {
                try
                {
                    Init(defaultModelPath, nCtx, nThreads);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    OnReadyChanged?.Invoke(false);
                }
            }
        }

        private void Start()
        {
            EnsureGateway();
        }

        public void Init(string modelPath, int nCtx, int nThreads)
        {
            string path = ResolveModelPath(modelPath);

            if (Runtime != null)
            {
                try
                {
                    Runtime.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                Runtime = null;
                OnReadyChanged?.Invoke(false);
            }

            Runtime = new LocalLLMRuntime();
            cachedTemplatePrefix = null;
            _gateway = null;

            try
            {
                Runtime.Init(path, nCtx, nThreads);

                string sha = null;

                try
                {
                    using var fs = File.OpenRead(path);
                    using var sha256 = SHA256.Create();

                    byte[] h = sha256.ComputeHash(fs);
                    sha = BitConverter.ToString(h).Replace("-", "").ToLowerInvariant();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LLM-Host] SHA256 compute failed: {ex.GetType().Name} {ex.Message}");
                    sha = "";
                }

                const string arch = "arm64-v8a";
                const string buildId = "v2.3.0";

                int vrc = -999;

                try
                {
                    vrc = ShimNative.llama_unity_verify_model(sha, arch, buildId);
                }
                catch (Exception vex)
                {
                    Debug.LogError($"[LLM-Host] verify entrypoint failed: {vex.GetType().Name} {vex.Message}");
                    vrc = -998;
                }

                if (vrc < 0)
                {
                    Debug.LogError($"[LLM-Host] verify failed rc={vrc} sha={sha}");
                    OnReadyChanged?.Invoke(false);
                    throw new InvalidOperationException($"Model verify failed rc={vrc}");
                }

                Runtime.SetResetCtxEachTurn(resetCtxEachTurn);

                this.nCtx = nCtx;
                this.nThreads = nThreads;

                Debug.Log($"[LLM-Host] Initialized: n_ctx={nCtx} n_threads={nThreads} model={path}");

                string info = Runtime.ModelInfoJson ?? string.Empty;
                byte[] pre = Encoding.UTF8.GetBytes(info);
                Debug.Log($"[LLM-Host] model_info: len={pre.Length} sha1={Sha1(pre)}");

                OnReadyChanged?.Invoke(true);

                EnsureGateway();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLM-Host] Init failed: {ex.Message}");
                Debug.LogException(ex);
                OnReadyChanged?.Invoke(false);
                throw;
            }
        }

        public void InitIfNeeded()
        {
            if (Runtime != null && Runtime.Initialized)
                return;

            Debug.Log("[LLM-Host] InitIfNeeded: initializing with default model");

            try
            {
                Init(defaultModelPath, nCtx, nThreads);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                OnReadyChanged?.Invoke(false);
            }
        }

        public void ReloadModel(string modelPath, int nCtxOverride = -1, int nThreadsOverride = -1)
        {
            StartCoroutine(CoReloadModel(
                modelPath,
                nCtxOverride > 0 ? nCtxOverride : nCtx,
                nThreadsOverride > 0 ? nThreadsOverride : nThreads
            ));
        }

        private IEnumerator CoReloadModel(string modelPath, int newNCtx, int newNThreads)
        {
            OnReadyChanged?.Invoke(false);
            _gateway = null;

            yield return null;

            try
            {
                Init(modelPath, newNCtx, newNThreads);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public LocalLLMRuntime.SamplingParams GetSampling()
        {
            var sp = LocalLLMRuntime.SamplingParams.CreateDefault();

            sp.top_k = top_k;
            sp.top_p = top_p;
            sp.min_p = min_p;
            sp.temperature = temperature;
            sp.seed = seed;
            sp.n_keep = n_keep;

            sp.repeat_penalty = repeat_penalty;
            sp.repeat_last_n = repeat_last_n;
            sp.presence_penalty = presence_penalty;

            return sp;
        }

        private LocalLLMRuntime.SamplingParams GetSampling(LLMGenerationProfile profile)
        {
            var sp = GetSampling();

            if (profile == null)
                return sp;

            sp.top_k = profile.topK;
            sp.top_p = profile.topP;
            sp.temperature = profile.temperature;
            sp.repeat_penalty = profile.repeatPenalty;

            return sp;
        }

        public void GenerateAsyncEx2U8(
            string prompt,
            LocalLLMRuntime.SamplingParams sp,
            string grammar,
            bool strictJsonFlag,
            string stopCsvArg,
            Action<string> onDone)
        {
            if (Runtime == null || !Runtime.Initialized)
                throw new InvalidOperationException("Runtime is not initialized.");

            StartCoroutine(CoGenerate(
                prompt ?? string.Empty,
                maxTokens,
                sp,
                onDone,
                stopCsvArg,
                strictJsonFlag
            ));
        }

        public void GenerateAsyncEx2U8(
            string prompt,
            int nPredict,
            LocalLLMRuntime.SamplingParams sp,
            string grammar,
            bool strictJsonFlag,
            Action<string> onDone)
        {
            if (Runtime == null || !Runtime.Initialized)
                throw new InvalidOperationException("Runtime is not initialized.");

            StartCoroutine(CoGenerate(
                prompt ?? string.Empty,
                nPredict,
                sp,
                onDone,
                null,
                strictJsonFlag
            ));
        }

        public void GenerateAsyncEx2U8(
            string prompt,
            int nPredict,
            LocalLLMRuntime.SamplingParams sp,
            string grammar,
            bool strictJsonFlag,
            string stopCsvArg,
            Action<string> onDone)
        {
            if (Runtime == null || !Runtime.Initialized)
                throw new InvalidOperationException("Runtime is not initialized.");

            StartCoroutine(CoGenerate(
                prompt ?? string.Empty,
                nPredict,
                sp,
                onDone,
                stopCsvArg,
                strictJsonFlag
            ));
        }

        private IEnumerator CoGenerate(
            string prompt,
            int nPredict,
            LocalLLMRuntime.SamplingParams sp,
            Action<string> onDone,
            string stopCsvArg = null,
            bool strictJsonFlag = false)
        {
            Debug.Log("[LLM-Host] CoGenerate ENTER");

            string norm = NormalizeForPrompt(prompt);

            string finalPrompt = null;
            bool usedModelTemplate = false;

            if (useModelTemplate)
                cachedTemplatePrefix = null;

            if (useModelTemplate && Runtime != null)
            {
                if (string.IsNullOrEmpty(cachedTemplatePrefix))
                {
                    try
                    {
                        string sys = string.IsNullOrWhiteSpace(systemPrompt) ? null : systemPrompt;

                        string rendered = Runtime.ApplyChatTemplateAuto(
                            sys,
                            norm,
                            addAssistant: true
                        );

                        if (!string.IsNullOrEmpty(rendered))
                        {
                            finalPrompt = rendered;
                            usedModelTemplate = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LLM-Host] model template apply failed: {ex.GetType().Name} {ex.Message} (fallback)");
                    }
                }
            }

            if (string.IsNullOrEmpty(finalPrompt))
            {
                finalPrompt = applySimpleChatTemplate
                    ? BuildSimpleChat(norm, roleUser, roleAssistant)
                    : norm;
            }

            byte[] preBytes = Encoding.UTF8.GetBytes(finalPrompt);
            Debug.Log($"[LLM-Host] pre-invoke: len={preBytes.Length} sha1={Sha1(preBytes)} tmpl={(usedModelTemplate ? "model" : (applySimpleChatTemplate ? "simple" : "none"))}");

            string final = string.Empty;
            float t0 = Time.realtimeSinceStartup;

            try
            {
                string activeStopCsv = stopCsvArg ?? stopCsv;
                string antiPromptsJson = CsvToJsonArray(activeStopCsv);
                bool useStrict = strictJsonFlag || strictJson;

                Debug.Log($"[LLM-Host] invoke: strictJson={useStrict} stops={(antiPromptsJson ?? "null")} rep={sp.repeat_penalty} last_n={sp.repeat_last_n} pres={sp.presence_penalty}");

                final = Runtime.GenerateEx2(
                    finalPrompt,
                    nPredict,
                    sp,
                    antiPromptsJson,
                    useStrict
                ) ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError("[LLM-Host] GenerateEx2 threw exception");
                Debug.LogException(ex);
                final = string.Empty;
            }

            byte[] postBytes = Encoding.UTF8.GetBytes(final);
            float ms = (Time.realtimeSinceStartup - t0) * 1000f;

            Debug.Log($"[LLM-Host] post-invoke: len={postBytes.Length} sha1={Sha1(postBytes)} time_ms={(int)ms}");

            try
            {
                final = StopWordsMatcherUtf8.CutAtFirstStop(final, stopCsvArg ?? stopCsv);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLM-Host] StopWords cut failed: {ex.GetType().Name} {ex.Message}");
            }

            final = Regex.Replace(
                final,
                @"(?<=(\p{IsHiragana}|\p{IsKatakana}|\p{IsCJKUnifiedIdeographs}))\?(?=(\p{IsHiragana}|\p{IsKatakana}|\p{IsCJKUnifiedIdeographs}))",
                ""
            );

            Debug.Log($"[LLM-Host] CoGenerate DONE len={final.Length}");

            onDone?.Invoke(final);
            yield break;
        }

        public void SetResetCtxEachTurn(bool enable)
        {
            resetCtxEachTurn = enable;

            if (Runtime != null)
            {
                try
                {
                    Runtime.SetResetCtxEachTurn(enable);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        private void EnsureGateway()
        {
            if (_gateway != null)
                return;

            Debug.Log("[LLM-Host] EnsureGateway ENTER");

            if (Runtime == null || !Runtime.Initialized)
            {
                Debug.LogWarning("[LLM-Host] EnsureGateway: Runtime is not ready");
                return;
            }

            if (FindObjectOfType<ResumeHook>() == null)
            {
                var go = new GameObject("ResumeHook");
                go.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
                go.AddComponent<ResumeHook>();
            }

            _gateway = new GenerationGateway(
                ctxProvider: () => Runtime != null ? Runtime.GetContextPtr() : IntPtr.Zero,
                nativeGenerateAsync: prompt =>
                {
                    Debug.Log("[LLM-Host] nativeGenerateAsync ENTER");

                    var tcs = new TaskCompletionSource<string>(
                        TaskCreationOptions.RunContinuationsAsynchronously
                    );

                    try
                    {
                        var sp = GetSampling();

                        GenerateAsyncEx2U8(
                            prompt,
                            sp,
                            null,
                            false,
                            stopCsv,
                            result =>
                            {
                                Debug.Log($"[LLM-Host] nativeGenerateAsync DONE len={(result ?? string.Empty).Length}");
                                tcs.TrySetResult(result ?? string.Empty);
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LLM-Host] nativeGenerateAsync exception: {ex.GetType().Name} {ex.Message}");
                        Debug.LogException(ex);
                        tcs.TrySetResult(string.Empty);
                    }

                    return tcs.Task;
                }
            );

            Debug.Log("[LLM-Host] Gateway created");
        }

        public async Task<string> GenerateSafeAsync(string prompt)
        {
            Debug.Log("[LLM-Host] GenerateSafeAsync ENTER");

            await generateLock.WaitAsync();

            try
            {
                return await GenerateSafeInternalAsync(
                    prompt,
                    "default"
                );
            }
            finally
            {
                generateLock.Release();
            }
        }

        public async Task<string> GenerateSafeAsync(
            string prompt,
            LLMGenerationProfile profile,
            string profileKey)
        {
            string safeProfileKey = BuildProfileKey(profile, profileKey);

            Debug.Log($"[LLM-Host] GenerateSafeAsync PROFILE ENTER key={safeProfileKey}");

            await generateLock.WaitAsync();

            int oldMaxTokens = maxTokens;
            int oldTopK = top_k;
            float oldTopP = top_p;
            float oldTemperature = temperature;
            float oldRepeatPenalty = repeat_penalty;

            try
            {
                if (profile != null)
                {
                    maxTokens = Mathf.Max(1, profile.maxTokens);
                    top_k = Mathf.Max(1, profile.topK);
                    top_p = Mathf.Clamp01(profile.topP);
                    temperature = Mathf.Clamp(profile.temperature, 0.0f, 2.0f);
                    repeat_penalty = Mathf.Max(0.1f, profile.repeatPenalty);

                    Debug.Log(
                        "[LLM-Host] Profile applied " +
                        $"key={safeProfileKey} " +
                        $"maxTokens={maxTokens} temp={temperature} " +
                        $"topK={top_k} topP={top_p} rep={repeat_penalty}"
                    );
                }

                return await GenerateSafeInternalAsync(
                    prompt,
                    safeProfileKey
                );
            }
            finally
            {
                maxTokens = oldMaxTokens;
                top_k = oldTopK;
                top_p = oldTopP;
                temperature = oldTemperature;
                repeat_penalty = oldRepeatPenalty;

                Debug.Log("[LLM-Host] Profile restored");

                generateLock.Release();
            }
        }

        private async Task<string> GenerateSafeInternalAsync(string prompt, string profileKey)
        {
            if (Runtime == null || !Runtime.Initialized)
            {
                Debug.LogWarning("[LLM-Host] GenerateSafeAsync: Runtime not ready. InitIfNeeded.");
                InitIfNeeded();
            }

            if (_gateway == null)
            {
                Debug.LogWarning("[LLM-Host] GenerateSafeAsync: gateway is null. EnsureGateway.");
                EnsureGateway();
            }

            if (_gateway == null)
            {
                Debug.LogError("[LLM-Host] GenerateSafeAsync: gateway create failed");
                return string.Empty;
            }

            try
            {
                string result = await _gateway.GenerateAsync(
                    prompt ?? string.Empty,
                    string.IsNullOrEmpty(profileKey) ? "default" : profileKey
                );

                Debug.Log($"[LLM-Host] GenerateSafeAsync DONE len={(result ?? string.Empty).Length}");
                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLM-Host] GenerateSafeAsync exception: {ex.GetType().Name} {ex.Message}");
                Debug.LogException(ex);
                return string.Empty;
            }
        }

        private static string BuildProfileKey(LLMGenerationProfile profile, string profileKey)
        {
            string baseKey = string.IsNullOrEmpty(profileKey)
                ? "profile"
                : profileKey;

            if (profile == null)
                return baseKey;

            return
                baseKey +
                $":mt{profile.maxTokens}" +
                $":tk{profile.topK}" +
                $":tp{profile.topP:F2}" +
                $":tm{profile.temperature:F2}" +
                $":rp{profile.repeatPenalty:F2}";
        }

        static string BuildSimpleChat(string user, string roleUser, string roleAssistant)
        {
            string ru = string.IsNullOrEmpty(roleUser) ? "User" : roleUser;
            string ra = string.IsNullOrEmpty(roleAssistant) ? "Assistant" : roleAssistant;

            var sb = new StringBuilder(user.Length + 32);
            sb.Append(ru).Append(": ").Append(user).Append('\n');
            sb.Append(ra).Append(":");
            return sb.ToString();
        }

        static string Sha1(byte[] data)
        {
            using var sha = SHA1.Create();

            byte[] h = sha.ComputeHash(data);
            var sb = new StringBuilder(h.Length * 2);

            foreach (byte b in h)
                sb.Append(b.ToString("x2"));

            return sb.ToString();
        }

        string ResolveModelPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(Application.persistentDataPath, path);
        }

        private void OnDestroy()
        {
            try
            {
                if (Runtime != null)
                {
                    Runtime.Dispose();
                    Runtime = null;
                }

                generateLock?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                OnReadyChanged?.Invoke(false);

                if (Instance == this)
                    Instance = null;
            }
        }

        static string NormalizeForPrompt(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            string nfc = s.Normalize(NormalizationForm.FormC);
            string noZw = RemoveZeroWidth(nfc);
            string compact = CompressNewlines(noZw, 2);

            return TrimOuter(compact);
        }

        static string RemoveZeroWidth(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\u2060' || c == '\uFEFF')
                    continue;

                sb.Append(c);
            }

            return sb.ToString();
        }

        static string CompressNewlines(string s, int maxConsecutive)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            int count = 0;
            var sb = new StringBuilder(s.Length);

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c == '\n')
                {
                    count++;

                    if (count <= maxConsecutive)
                        sb.Append(c);
                }
                else
                {
                    count = 0;
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        static string TrimOuter(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            return s.Trim(' ', '\t', '\r', '\n');
        }

        static string CsvToJsonArray(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return "[]";

            string[] parts = csv.Split(',');
            var sb = new StringBuilder();

            sb.Append('[');

            bool first = true;

            foreach (string raw in parts)
            {
                string s = raw?.Trim();

                if (string.IsNullOrEmpty(s))
                    continue;

                if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                    s = s.Substring(1, s.Length - 2);

                s = UnescapeStopToken(s);

                if (!first)
                    sb.Append(',');

                sb.Append('"').Append(JsonEscapeString(s)).Append('"');

                first = false;
            }

            sb.Append(']');

            return sb.ToString();
        }

        static string UnescapeStopToken(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            s = s.Replace("\\n", "\n");
            s = s.Replace("\\r", "\r");
            s = s.Replace("\\t", "\t");

            return s;
        }

        static string JsonEscapeString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var sb = new StringBuilder(s.Length + 8);

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                switch (c)
                {
                    case '\"':
                        sb.Append("\\\"");
                        break;

                    case '\\':
                        sb.Append("\\\\");
                        break;

                    case '\b':
                        sb.Append("\\b");
                        break;

                    case '\f':
                        sb.Append("\\f");
                        break;

                    case '\n':
                        sb.Append("\\n");
                        break;

                    case '\r':
                        sb.Append("\\r");
                        break;

                    case '\t':
                        sb.Append("\\t");
                        break;

                    default:
                        if (c < 0x20)
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        public static string DumpShimDiagnostics(int maxLines = 64)
        {
            var sb = new StringBuilder(16 * 1024);

            try
            {
                int n = ShimNative.llama_unity_dump_diagnostics(sb, sb.Capacity, maxLines);

                if (n > 0)
                    return sb.ToString();
            }
            catch
            {
                // ignore
            }

            return "(no diag)";
        }
    }
}