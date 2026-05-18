// WavUtility v2025-09-07-02
// Timestamp (JST): 2025-09-07 01:05
// Comment (最小改修・ログ強化):
// - 32bit Float (IEEE_FLOAT) 対応は維持（8/16bit PCM も維持）
// - ヘッダ解析時に詳細ログを追加：fmt名/format値/ch/sampleRate/bps/dataBytes/dataPos
// - データ→サンプル変換前に見積りサンプル数/フレーム数をログ出力
// - 例外/異常系のログを明確化（どこで落ちたか分かるように）
// - 公開API・シグネチャは変更しない（ToAudioClip）

using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    /// <summary>
    /// WAV (byte[]) を AudioClip に変換します。
    /// 対応: 8bit PCM, 16bit PCM, 32bit Float PCM
    /// </summary>
    public static AudioClip ToAudioClip(byte[] wavData, string name = "wav")
    {
        if (wavData == null || wavData.Length < 44)
        {
            Debug.LogWarning("[WavUtility] Invalid WAV data (null or too small).");
            return null;
        }

        try
        {
            using (var ms = new MemoryStream(wavData))
            using (var br = new BinaryReader(ms))
            {
                // --- RIFF ヘッダ ---
                if (br.ReadByte() != 'R' || br.ReadByte() != 'I' || br.ReadByte() != 'F' || br.ReadByte() != 'F')
                {
                    Debug.LogWarning("[WavUtility] Not a RIFF file.");
                    return null;
                }

                int riffSize = br.ReadInt32(); // fileSize (unused)
                if (br.ReadByte() != 'W' || br.ReadByte() != 'A' || br.ReadByte() != 'V' || br.ReadByte() != 'E')
                {
                    Debug.LogWarning("[WavUtility] Not a WAVE file.");
                    return null;
                }

                // --- チャンク探索 ---
                short audioFormat = 1;     // 1=PCM, 3=IEEE_FLOAT
                short numChannels = 1;
                int sampleRate = 44100;
                short bitsPerSample = 16;

                int dataChunkPos = -1;
                int dataChunkSize = -1;

                while (ms.Position + 8 <= ms.Length)
                {
                    long chunkStart = ms.Position;
                    int chunkId = br.ReadInt32();     // little-endian
                    int chunkSize = br.ReadInt32();

                    if (chunkId == 0x20746D66) // "fmt "
                    {
                        long fmtStart = ms.Position;

                        audioFormat = br.ReadInt16();   // 1=PCM, 3=IEEE Float
                        numChannels = br.ReadInt16();
                        sampleRate = br.ReadInt32();
                        int byteRate = br.ReadInt32();
                        short align = br.ReadInt16();
                        bitsPerSample = br.ReadInt16();

                        // 拡張がある場合はスキップ
                        long readBytes = ms.Position - fmtStart;
                        long remain = chunkSize - readBytes;
                        if (remain > 0) ms.Position += remain;

                        string fmtName = audioFormat == 1 ? "PCM" : (audioFormat == 3 ? "FLOAT" : $"UNKNOWN({audioFormat})");
                        Debug.Log($"[WavUtility] fmt chunk: fmt={fmtName}({audioFormat}), ch={numChannels}, rate={sampleRate}, bps={bitsPerSample}, byteRate={byteRate}, align={align}, size={chunkSize}");
                    }
                    else if (chunkId == 0x61746164) // "data"
                    {
                        dataChunkPos = (int)ms.Position;
                        dataChunkSize = chunkSize;
                        ms.Position += chunkSize;
                        Debug.Log($"[WavUtility] data chunk: pos={dataChunkPos}, size={dataChunkSize}");
                    }
                    else
                    {
                        // その他のチャンク（fact/list/etc）はスキップ
                        ms.Position += chunkSize;
                        Debug.Log($"[WavUtility] skip chunk id=0x{chunkId:X8}, size={chunkSize}, start={chunkStart}");
                    }
                }

                if (dataChunkPos < 0 || dataChunkSize <= 0)
                {
                    Debug.LogWarning("[WavUtility] data chunk not found or empty.");
                    return null;
                }

                // データ読み込み
                ms.Position = dataChunkPos;
                byte[] pcm = br.ReadBytes(dataChunkSize);

                // 想定サンプル数の見積もりログ
                string modeName = audioFormat == 1 ? (bitsPerSample + "bit PCM") :
                                  (audioFormat == 3 && bitsPerSample == 32 ? "32bit FLOAT" : $"fmt={audioFormat}/bps={bitsPerSample}");
                Debug.Log($"[WavUtility] decode plan: mode={modeName}, rawBytes={pcm.Length}");

                // --- PCM を float[] に展開 ---
                float[] samples;

                if (audioFormat == 1) // PCM 固定小数点 (8/16)
                {
                    if (bitsPerSample == 8)
                    {
                        int totalSamples = pcm.Length; // 8bit=1byte/サンプル（チャンネル合算）
                        int frames = Mathf.Max(1, totalSamples / Mathf.Max(1, numChannels));
                        Debug.Log($"[WavUtility] 8bit PCM: totalSamples={totalSamples}, frames={frames}, ch={numChannels}");
                        samples = new float[totalSamples];
                        for (int i = 0; i < totalSamples; i++)
                            samples[i] = (pcm[i] - 128) / 128f;
                    }
                    else if (bitsPerSample == 16)
                    {
                        if (pcm.Length % 2 != 0)
                        {
                            Debug.LogWarning("[WavUtility] 16bit PCM but odd bytes.");
                            return null;
                        }
                        int totalSamples = pcm.Length / 2;
                        int frames = Mathf.Max(1, totalSamples / Mathf.Max(1, numChannels));
                        Debug.Log($"[WavUtility] 16bit PCM: totalSamples={totalSamples}, frames={frames}, ch={numChannels}");
                        samples = new float[totalSamples];
                        for (int i = 0; i < totalSamples; i++)
                        {
                            short v = BitConverter.ToInt16(pcm, i * 2);
                            samples[i] = v / 32768f;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[WavUtility] Unsupported PCM bitsPerSample: {bitsPerSample}");
                        return null;
                    }
                }
                else if (audioFormat == 3 && bitsPerSample == 32) // IEEE FLOAT 32bit
                {
                    if (pcm.Length % 4 != 0)
                    {
                        Debug.LogWarning("[WavUtility] 32bit float but bytes not multiple of 4.");
                        return null;
                    }
                    int totalSamples = pcm.Length / 4;
                    int frames = Mathf.Max(1, totalSamples / Mathf.Max(1, numChannels));
                    Debug.Log($"[WavUtility] 32bit FLOAT: totalSamples={totalSamples}, frames={frames}, ch={numChannels}");
                    samples = new float[totalSamples];
                    // WAV は LE 前提：Buffer.BlockCopy で float[] へ
                    Buffer.BlockCopy(pcm, 0, samples, 0, pcm.Length);
                }
                else
                {
                    Debug.LogWarning($"[WavUtility] Unsupported format: audioFormat={audioFormat}, bitsPerSample={bitsPerSample}");
                    return null;
                }

                // --- AudioClip 生成 ---
                int channels = Mathf.Max(1, numChannels);
                int totalFrames = samples.Length / channels;
                if (totalFrames <= 0)
                {
                    Debug.LogWarning("[WavUtility] totalFrames <= 0 (no audio).");
                    return null;
                }

                var clip = AudioClip.Create(name ?? "wav", totalFrames, channels, Mathf.Max(8000, sampleRate), false);
                clip.SetData(samples, 0);
                Debug.Log($"[WavUtility] AudioClip created: frames={totalFrames}, ch={channels}, rate={sampleRate}");
                return clip;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WavUtility] ToAudioClip exception: {ex.Message}");
            return null;
        }
    }
}
