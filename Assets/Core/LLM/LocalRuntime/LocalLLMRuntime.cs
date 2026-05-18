// Version: LocalLLMRuntimeV2.cs v1.10.1 (UTF8 Safe Decode w/o Re-run)
// Timestamp (JST): 2025-10-10 13:45
// Comment:
// - 変更点: UTF-8不完全検知で "再実行(再生成)" せず、その場で安全デコードへフォールバック。
// - 目的: 再実行で rc=-10 (no output) に落ちる経路を排除。返答が空になる問題を解消。
// - 方針: 既存API/構造は不変更。Stop語やテンプレ処理も不変更。
// - 備考: rc=-11 のコンテキスト再初期化リトライは従来どおり1回のみ維持。

using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace SalieriAI.AndroidLLM
{
    public sealed class LocalLLMRuntime : IDisposable
    {
        private const string TAG = "[LLM-Runtime]";

        public struct SamplingParams
        {
            public int top_k;
            public float top_p;
            public float min_p;
            public float temperature;
            public int seed;
            public int n_keep;

            public float repeat_penalty;
            public int repeat_last_n;
            public float presence_penalty;

            public static SamplingParams CreateDefault()
            {
                return new SamplingParams
                {
                    top_k = 40,
                    top_p = 0.95f,
                    min_p = 0.05f,
                    temperature = 0.7f,
                    seed = -1,
                    n_keep = 0,

                    repeat_penalty = 1.10f,
                    repeat_last_n = 64,
                    presence_penalty = 0.10f
                };
            }

            public void ToSafe()
            {
                if (temperature <= 0f && top_k <= 0 && top_p <= 0f && min_p <= 0f)
                {
                    temperature = 0.10f;
                    Debug.Log($"{TAG} Sampling.ToSafe: coerced to temp=0.10");
                }
                if (repeat_penalty < 1.0f) repeat_penalty = 1.0f;
                if (repeat_last_n < 0) repeat_last_n = 0;
                if (presence_penalty < 0f) presence_penalty = 0f;
            }
        }

        public bool Initialized { get; private set; }
        public int NThreads => _nThreads;
        public int NCtx => _nCtx;

        public string ModelInfoJson { get; private set; } = string.Empty;

        private IntPtr Model = IntPtr.Zero;
        private IntPtr Ctx = IntPtr.Zero;
        private int _nCtx, _nThreads;

        public bool ResetCtxEachTurn { get; private set; } = true;
        public void SetResetCtxEachTurn(bool enable)
        {
            ResetCtxEachTurn = enable;
            Debug.Log($"{TAG} SetResetCtxEachTurn: {enable}");
        }

        private void ReinitCtx()
        {
            if (!Initialized || Model == IntPtr.Zero)
            {
                Debug.LogWarning($"{TAG} ReinitCtx: skip (Initialized={Initialized})");
                return;
            }
            try
            {
                if (Ctx != IntPtr.Zero)
                {
                    try { LlamaShimBindings.unity_llama_free_context(Ctx); }
                    catch (EntryPointNotFoundException epnf) { Debug.LogWarning($"{TAG} ReinitCtx: missing free_context {epnf.Message}"); }
                    catch (Exception ex) { Debug.LogWarning($"{TAG} ReinitCtx: free_context {ex.GetType().Name} {ex.Message}"); }
                    finally { Ctx = IntPtr.Zero; }
                }

                IntPtr newCtx = IntPtr.Zero;
                try { newCtx = LlamaShimBindings.unity_llama_new_context_ex(Model, _nCtx, _nThreads); }
                catch (EntryPointNotFoundException) { newCtx = LlamaShimBindings.unity_llama_new_context_default(Model, _nCtx, _nThreads); }

                if (newCtx == IntPtr.Zero) Debug.LogError($"{TAG} ReinitCtx: new_context failed");
                else { Ctx = newCtx; Debug.Log($"{TAG} ReinitCtx: new context n_ctx={_nCtx} n_threads={_nThreads}"); }
            }
            catch (Exception ex) { Debug.LogError($"{TAG} ReinitCtx exception {ex.GetType().Name}: {ex.Message}"); }
        }

        public void Init(string modelPath, int nCtx, int nThreads)
        {
            if (Initialized)
            {
                Debug.Log($"{TAG} Init: already initialized (skip)");
                return;
            }

            _nCtx = nCtx;
            _nThreads = nThreads;
            Debug.Log($"{TAG} Init: begin model=\"{modelPath}\" n_ctx={nCtx} n_threads={nThreads}");

            IntPtr model = IntPtr.Zero;
            try
            {
                try { model = LlamaShimBindings.unity_llama_load_model_ex(modelPath, nThreads); }
                catch (EntryPointNotFoundException) { model = LlamaShimBindings.unity_llama_load_model_default(modelPath, nThreads); }
            }
            catch (Exception e)
            {
                Debug.LogError($"{TAG} Init: load_model failed {e.GetType().Name} {e.Message}");
                throw;
            }

            if (model == IntPtr.Zero) { Debug.LogError($"{TAG} Init: load_model returned null"); throw new InvalidOperationException("load_model failed"); }
            Model = model;
            Debug.Log($"{TAG} load_model: completed (Model!=0)");

            IntPtr ctx = IntPtr.Zero;
            try
            {
                try { ctx = LlamaShimBindings.unity_llama_new_context_ex(Model, nCtx, nThreads); }
                catch (EntryPointNotFoundException) { ctx = LlamaShimBindings.unity_llama_new_context_default(Model, nCtx, nThreads); }
            }
            catch (Exception)
            {
                try { if (Model != IntPtr.Zero) LlamaShimBindings.unity_llama_free_model(Model); } catch { }
                Model = IntPtr.Zero;
                throw;
            }

            if (ctx == IntPtr.Zero)
            {
                try { if (Model != IntPtr.Zero) LlamaShimBindings.unity_llama_free_model(Model); } catch { }
                Model = IntPtr.Zero;
                throw new InvalidOperationException("new_context failed");
            }
            Ctx = ctx;
            Debug.Log($"{TAG} new_context: completed (Ctx!=0)");

            Initialized = true;
            try
            {
                var buf = new byte[32768];
                int need = LlamaShimBindings.unity_llama_model_info_json(Model, buf, buf.Length);
                if (need > 0) { int n = Math.Min(need, buf.Length); ModelInfoJson = Encoding.UTF8.GetString(buf, 0, n); }
                Debug.Log($"{TAG} ModelInfo: size={need}");
            }
            catch (Exception ex) { Debug.LogWarning($"{TAG} ModelInfo: failed {ex.GetType().Name} {ex.Message}"); }
            Debug.Log($"{TAG} Init done model=\"{modelPath}\" n_ctx={nCtx} n_threads={nThreads}");
        }

        public string ApplyChatTemplateAuto(string system, string user, bool addAssistant)
        {
            if (!Initialized || Model == IntPtr.Zero)
            {
                Debug.LogWarning($"{TAG} ApplyChatTemplateAuto: not initialized");
                return null;
            }

            int n = string.IsNullOrEmpty(system) ? 1 : 2;
            var msgs = new LlamaShimBindings.LlamaChatMessage[n];

            IntPtr pRole0 = IntPtr.Zero, pCont0 = IntPtr.Zero;
            IntPtr pRole1 = IntPtr.Zero, pCont1 = IntPtr.Zero;

            try
            {
                if (!string.IsNullOrEmpty(system))
                {
                    pRole0 = AllocUtf8("system");
                    pCont0 = AllocUtf8(system);
                    msgs[0].role = pRole0; msgs[0].content = pCont0;

                    pRole1 = AllocUtf8("user");
                    pCont1 = AllocUtf8(user ?? string.Empty);
                    msgs[1].role = pRole1; msgs[1].content = pCont1;
                }
                else
                {
                    pRole0 = AllocUtf8("user");
                    pCont0 = AllocUtf8(user ?? string.Empty);
                    msgs[0].role = pRole0; msgs[0].content = pCont0;
                }

                var buf = new byte[65536];
                int rc = LlamaShimBindings.shim_chat_apply_auto(Model, msgs, (UIntPtr)msgs.Length, addAssistant, buf, buf.Length);
                if (rc <= 0) return null;

                int w = Math.Min(rc, buf.Length);
                string rendered = Encoding.UTF8.GetString(buf, 0, w);
                Debug.Log($"{TAG} TemplateApplied: bytes={w}");
                return rendered;
            }
            catch (EntryPointNotFoundException)
            {
                Debug.LogWarning($"{TAG} Template apply not available (EntryPointNotFound)");
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"{TAG} Template apply failed: {ex.GetType().Name} {ex.Message}");
                return null;
            }
            finally
            {
                if (pRole0 != IntPtr.Zero) Marshal.FreeHGlobal(pRole0);
                if (pCont0 != IntPtr.Zero) Marshal.FreeHGlobal(pCont0);
                if (pRole1 != IntPtr.Zero) Marshal.FreeHGlobal(pRole1);
                if (pCont1 != IntPtr.Zero) Marshal.FreeHGlobal(pCont1);
            }
        }

        private static IntPtr AllocUtf8(string s)
        {
            if (s == null) s = string.Empty;
            byte[] b = Encoding.UTF8.GetBytes(s);
            IntPtr p = Marshal.AllocHGlobal(b.Length + 1);
            Marshal.Copy(b, 0, p, b.Length);
            Marshal.WriteByte(p, b.Length, 0);
            return p;
        }

        public string GenerateEx2(string prompt, int maxTokens, SamplingParams sp)
        {
            return GenerateEx2(prompt, maxTokens, sp, antiPromptsJson: null, strictJson: false);
        }

        public string GenerateEx2(string prompt, int maxTokens, SamplingParams sp, string antiPromptsJson, bool strictJson)
        {
            if (ResetCtxEachTurn)
            {
                Debug.Log($"{TAG} GEN: ResetCtxEachTurn=true -> ReinitCtx()");
                ReinitCtx();
            }

            if (!Initialized) throw new InvalidOperationException("Runtime not initialized");
            if (prompt == null) prompt = string.Empty;

            byte[] promptU8 = Encoding.UTF8.GetBytes(prompt);
            int promptLen = promptU8.Length;
            Debug.Log($"{TAG} GEN prep: promptLen={promptLen} strictJson={strictJson} stops={(antiPromptsJson ?? "null")}");

            sp.ToSafe();
            int nThreads = Mathf.Max(1, _nThreads);

            // 生成呼び出し（1回のみ）。バッファは余裕を持つ（CJK考慮で48B/トークン）。
            int outCap = Math.Max(4096, Math.Max(1, maxTokens) * 48);
            byte[] outBuf = new byte[outCap];

            int rc = 0;
            try
            {
                rc = LlamaShimBindings.unity_llama_generate_ex2_u8(
                    Model, Ctx,
                    promptU8, promptLen,
                    Mathf.Max(1, maxTokens),
                    nThreads,
                    sp.temperature,
                    sp.top_k,
                    sp.top_p,
                    sp.min_p,
                    sp.seed,
                    sp.repeat_penalty,
                    sp.repeat_last_n,
                    sp.presence_penalty,
                    sp.n_keep,
                    antiPromptsJson,
                    outBuf, outCap
                );
            }
            catch (EntryPointNotFoundException e)
            {
                Debug.LogError($"{TAG} GEN call: entrypoint missing unity_llama_generate_ex2_u8");
                throw new InvalidOperationException("generate_ex2_u8 entrypoint missing", e);
            }

            if (rc == -11)
            {
                // 互換: 旧来の一度だけの再初期化リトライは維持
                Debug.LogWarning($"{TAG} GEN retry: rc=-11 -> ReinitCtx() and retry once");
                ReinitCtx();
                try
                {
                    rc = LlamaShimBindings.unity_llama_generate_ex2_u8(
                        Model, Ctx,
                        promptU8, promptLen,
                        Mathf.Max(1, maxTokens),
                        nThreads,
                        sp.temperature,
                        sp.top_k,
                        sp.top_p,
                        sp.min_p,
                        sp.seed,
                        sp.repeat_penalty,
                        sp.repeat_last_n,
                        sp.presence_penalty,
                        sp.n_keep,
                        antiPromptsJson,
                        outBuf, outCap
                    );
                }
                catch (Exception rex)
                {
                    Debug.LogError($"{TAG} GEN retry exception: {rex.GetType().Name} {rex.Message}");
                    rc = -999;
                }
            }

            if (rc <= 0)
            {
                Debug.LogWarning($"{TAG} GEN result: rc={rc} (no output)");
                return string.Empty;
            }

            int n = Mathf.Min(rc, outCap);

            // ==== ここからUTF-8安全デコード（“再実行”はもうしない） ====
            // 1) 厳格UTF-8（例外）→ 2) 最長妥当プレフィクス → 3) 置換ありUTF-8
            string finalText = TryDecodeUtf8PreferStrictThenPrefix(outBuf, n, out int usedBytes);

            Debug.Log($"{TAG} GEN result: rc={rc}, usedBytes={usedBytes}, textLen={finalText.Length}");
            return finalText;
        }

        public void Dispose()
        {
            Debug.Log($"{TAG} Dispose: begin");
            try
            {
                if (Ctx != IntPtr.Zero)
                {
                    try { LlamaShimBindings.unity_llama_free_context(Ctx); Debug.Log($"{TAG} Dispose: context freed"); }
                    catch (EntryPointNotFoundException) { Debug.LogWarning($"{TAG} Dispose: free_context missing"); }
                    catch (Exception ex) { Debug.LogWarning($"{TAG} Dispose: free_context {ex.GetType().Name} {ex.Message}"); }
                    finally { Ctx = IntPtr.Zero; }
                }
                if (Model != IntPtr.Zero)
                {
                    try { LlamaShimBindings.unity_llama_free_model(Model); Debug.Log($"{TAG} Dispose: model freed"); }
                    catch (Exception ex) { Debug.LogWarning($"{TAG} Dispose: free_model threw {ex.GetType().Name}: {ex.Message}"); }
                    finally { Model = IntPtr.Zero; }
                }
            }
            catch (Exception ex) { Debug.LogException(ex); }

            Initialized = false;
            Debug.Log($"{TAG} Dispose: completed");
        }

        /// <summary>
        /// 現在の llama_context* ポインタを返す（nullなら IntPtr.Zero）
        /// </summary>
        public IntPtr GetContextPtr()
        {
            return Ctx;
        }

        // ===== UTF-8ユーティリティ =====

        private static string TryDecodeUtf8PreferStrictThenPrefix(byte[] buf, int len, out int usedBytes)
        {
            usedBytes = 0;
            if (len <= 0) return string.Empty;

            // (1) 厳格UTF-8
            var strict = new UTF8Encoding(false, true);
            try
            {
                string s = strict.GetString(buf, 0, len);
                usedBytes = len;
                return s;
            }
            catch { /* fallthrough */ }

            // (2) 先頭からの最長妥当プレフィクス
            int valid = FindLongestValidUtf8Prefix(buf, len);
            if (valid > 0)
            {
                try
                {
                    string s2 = strict.GetString(buf, 0, valid);
                    usedBytes = valid;
                    Debug.LogWarning($"{TAG} UTF8 prefix cut used (valid={valid}/{len})");
                    return s2;
                }
                catch { /* ここで失敗することは稀 */ }
            }

            // (3) 置換ありUTF-8（絶対に空返しにしない）
            usedBytes = len;
            return Encoding.UTF8.GetString(buf, 0, len);
        }

        // 先頭からスキャンして最長の正当UTF-8長
        private static int FindLongestValidUtf8Prefix(byte[] bytes, int count)
        {
            int i = 0;
            int end = count;
            while (i < end)
            {
                byte b = bytes[i];

                if (b < 0x80) { i++; continue; }

                int need;
                if ((b & 0xE0) == 0xC0)
                {
                    // オーバーロング開始（0xC0/0xC1）は不正扱い→break
                    if ((b & 0xFE) == 0xC0) break;
                    need = 1;
                }
                else if ((b & 0xF0) == 0xE0) { need = 2; }
                else if ((b & 0xF8) == 0xF0) { need = 3; }
                else { break; }

                if (i + need >= end) break;

                bool ok = true;
                for (int k = 1; k <= need; k++)
                {
                    byte c = bytes[i + k];
                    if ((c & 0xC0) != 0x80) { ok = false; break; }
                }
                if (!ok) break;

                i += (need + 1);
            }
            return i;
        }
    }
}
