// Version: AM2-LLamaShimBindings v1.2.0
// Timestamp (JST): 2025-10-09 09:30
// Comment:
// - 手順①対応：shim_chat_apply_auto の DllImport を “追加”。
// - 既存のエクスポートは一切変更せず、追加のみ（削らない・壊さない・混ぜない）。
// - C#側から model + (system/assistant/user) 配列を渡して C++側でテンプレ整形可能に。
// - marshalingは IL2CPP/Android を考慮し、bool は I1、size_t は UIntPtr を採用。
// - 追加：unity_llama_model_info_json DllImport

using System;
using System.Runtime.InteropServices;

internal static class LlamaShimBindings
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string DLL = "llama_unity_shim";
#else
    private const string DLL = "llama_unity_shim";
#endif

    // -------------------------------
    // 追加：chat テンプレート自動適用API
    // -------------------------------
    // C++: int32_t shim_chat_apply_auto(
    //   const llama_model* model,
    //   const llama_chat_message* msgs, size_t n_msg,
    //   bool add_assistant,
    //   char* out_buf, int32_t out_len);
    //
    // 返り値:
    //   >= 0 : 必要バイト数（buf不足時も必要サイズを返す。NUL終端は付与されない）
    //   -1   : テンプレ適用失敗/未検出
    //
    // 注意:
    //  - msgs は C 配列前提。役割(role)/本文(content)とも UTF-8 を想定。
    //  - 本バインディングでは LlamaChatMessage を最低限のポインタ構成で定義。
    //  - 役割/本文の UTF-8 バイト確保・解放は呼び出し側で管理してください。
    [DllImport(DLL, EntryPoint = "shim_chat_apply_auto", CallingConvention = CallingConvention.Cdecl)]
    public static extern int shim_chat_apply_auto(
        IntPtr model,
        [In] LlamaChatMessage[] msgs,
        UIntPtr n_msg,
        [MarshalAs(UnmanagedType.I1)] bool add_assistant,
        byte[] out_buf,
        int out_len
    );

    // C++ 側の llama_chat_message に合わせた最小構造体
    // struct llama_chat_message { const char* role; const char* content; };
    [StructLayout(LayoutKind.Sequential)]
    public struct LlamaChatMessage
    {
        public IntPtr role;    // const char* (UTF-8)
        public IntPtr content; // const char* (UTF-8)
    }

    // ------ model / context ------
    [DllImport(DLL, EntryPoint = "unity_llama_load_model_default", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_load_model_default(string modelPath, int n_threads /*kept for ABI*/);

    [DllImport(DLL, EntryPoint = "unity_llama_load_model_ex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_load_model_ex(string modelPath, int n_threads /*kept for ABI*/);

    [DllImport(DLL, EntryPoint = "unity_llama_load_model", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_load_model(string modelPath, int n_threads /*kept for ABI*/);

    [DllImport(DLL, EntryPoint = "unity_llama_new_context_default", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_new_context_default(IntPtr model, int n_ctx, int n_threads);

    [DllImport(DLL, EntryPoint = "unity_llama_new_context_ex", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_new_context_ex(IntPtr model, int n_ctx, int n_threads);

    [DllImport(DLL, EntryPoint = "unity_llama_new_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr unity_llama_new_context(IntPtr model, int n_ctx, int n_threads);

    [DllImport(DLL, EntryPoint = "unity_llama_free_context", CallingConvention = CallingConvention.Cdecl)]
    public static extern void unity_llama_free_context(IntPtr ctx);

    [DllImport(DLL, EntryPoint = "unity_llama_free_model", CallingConvention = CallingConvention.Cdecl)]
    public static extern void unity_llama_free_model(IntPtr model);

    // ------ generate (UTF-8) ------
    [DllImport(DLL, EntryPoint = "unity_llama_generate_ex2_u8", CallingConvention = CallingConvention.Cdecl)]
    public static extern int unity_llama_generate_ex2_u8(
        IntPtr model,
        IntPtr ctx,
        byte[] prompt_u8,
        int prompt_len,
        int max_tokens,
        int n_threads,
        float temperature,
        int top_k,
        float top_p,
        float min_p,
        int seed,
        float penalty_repeat,
        int penalty_count,
        float penalty_present,
        int n_keep,
        string anti_prompts_json, // null 時は未使用（EOS 停止）
        byte[] out_buf,
        int out_cap
    );

    // ------ 追加：モデル情報JSON ------
    [DllImport(DLL, EntryPoint = "unity_llama_model_info_json", CallingConvention = CallingConvention.Cdecl)]
    public static extern int unity_llama_model_info_json(IntPtr model, byte[] outBuf, int outCap);
}
