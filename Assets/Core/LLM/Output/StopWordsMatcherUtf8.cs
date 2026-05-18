// Version: StopWordsMatcherUtf8.cs v1.0.0
// Timestamp (JST): 2025-10-12 15:22
// Comment:
// - 追加のみ（既存ソースは不変更）。UTF-8安全な停止語マッチャ。
// - CSVの停止語を正規化/エスケープ解決して保持し、最初に出現した箇所で安全にカットする。
// - 将来、トークン列ベース（llama_tokenize）に置換可能な構造。今はUTF-8/文字列境界で精密化。
// - 依存関係: なし（Unity標準のみ）。名前空間はAibouMaker.AndroidLLMに揃えた。
// - 使い方: 生成後の文字列に対して CutAtFirstStop(text, csv) を一発適用。

using System;
using System.Text;
using System.Collections.Generic;

namespace SalieriAI.AndroidLLM
{
    internal static class StopWordsMatcherUtf8
    {
        // CSV（\n,\r,\tエスケープ含む）→ 停止語リスト（空や重複は除去）
        public static List<string> ParseCsvStops(string csv)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(csv)) return list;

            var parts = csv.Split(',');
            foreach (var raw in parts)
            {
                var s = (raw ?? string.Empty).Trim();
                if (s.Length == 0) continue;

                // 先頭末尾の二重引用符を除去
                if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                    s = s.Substring(1, s.Length - 2);

                s = Unescape(s);
                if (s.Length == 0) continue;

                // 表層正規化（NFC）
                s = s.Normalize(NormalizationForm.FormC);

                // ゼロ幅を除去して比較安定化
                s = RemoveZeroWidth(s);

                if (!list.Contains(s))
                    list.Add(s);
            }
            return list;
        }

        // 生成テキストを停止語の「最初に現れた位置」でカット（見つからなければ原文返し）
        // 注意: 文字境界で切るためUTF-8破断は発生しない（stringはUTF-16コードポイント境界で安定）。
        public static string CutAtFirstStop(string text, string stopCsv)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            // 入力側も同様に表層正規化＋ゼロ幅除去
            string s = text.Normalize(NormalizationForm.FormC);
            s = RemoveZeroWidth(s);

            var stops = ParseCsvStops(stopCsv);
            if (stops.Count == 0) return s;

            int cut = -1;
            foreach (var sw in stops)
            {
                // 空は無視
                if (string.IsNullOrEmpty(sw)) continue;

                int idx = s.IndexOf(sw, StringComparison.Ordinal);
                if (idx >= 0)
                {
                    if (cut < 0 || idx < cut) cut = idx;
                }
            }

            return (cut >= 0) ? s.Substring(0, cut) : s;
        }

        private static string Unescape(string s)
        {
            // \n,\r,\t の最小限のみサポート（現状と整合）
            // バックスラッシュは二重解釈されやすいので最後にまとめて処理。
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < s.Length)
                {
                    char n = s[i + 1];
                    switch (n)
                    {
                        case 'n': sb.Append('\n'); i++; continue;
                        case 'r': sb.Append('\r'); i++; continue;
                        case 't': sb.Append('\t'); i++; continue;
                        case '\\': sb.Append('\\'); i++; continue;
                        default: sb.Append(c); continue;
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static string RemoveZeroWidth(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\u2060' || c == '\uFEFF') continue;
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
