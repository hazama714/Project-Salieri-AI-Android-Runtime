// Version: Salieri.SingleFlight v1.0.1
// Timestamp (JST): 2025-10-14 19:44
// Comment: 型安全な合流。Task<object>⇄Task<T>の不正キャストを廃止し、TCS辞書で結果を共有。

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Salieri.Infra
{
    /// <summary>
    /// 同一キーに対する非同期処理を1本に束ねる（重複起動を抑止し、結果を共有）。
    /// </summary>
    public sealed class SingleFlight
    {
        // keyごとに進行中の結果（objectで箱詰め）。完了後は辞書から除去。
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _inflight =
            new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        /// <summary>
        /// 同一keyでの多重呼び出しは、最初の実行結果を共有する。
        /// </summary>
        public Task<T> DoAsync<T>(string key, Func<Task<T>> factory)
        {
            // 新規TCSを作って登録を試みる
            var tcsNew = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcs = _inflight.GetOrAdd(key, tcsNew);

            // すでに誰かが走っている → そのTaskを待ってTにキャスト
            if (!ReferenceEquals(tcs, tcsNew))
            {
                return tcs.Task.ContinueWith(
                    t => (T)t.Result,
                    TaskScheduler.Default
                );
            }

            // ここに来たスレッドが“代表”として実行
            return RunAsync();

            async Task<T> RunAsync()
            {
                try
                {
                    var result = await factory().ConfigureAwait(false);
                    tcs.SetResult(result!);
                    return result;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                    throw;
                }
                finally
                {
                    _inflight.TryRemove(key, out _);
                }
            }
        }

        /// <summary> 明示的に破棄したい時に。合流は解除される。 </summary>
        public bool TryCancel(string key) => _inflight.TryRemove(key, out _);

        public void ClearAll() => _inflight.Clear();
    }
}
