using System.Text.RegularExpressions;

namespace SalieriAI.CloudLLM
{
    public static class CloudLLMActionParser
    {
        public static int ParseActionDigit(
            string text,
            int fallback = 0
        )
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            Match match = Regex.Match(text, "[0-3]");

            if (!match.Success)
                return fallback;

            return int.Parse(match.Value);
        }
    }
}