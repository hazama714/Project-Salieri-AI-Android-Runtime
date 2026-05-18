using System.Text;
using UnityEngine;

namespace SalieriAI.Persona
{
    public sealed class PersonaPromptLoader : MonoBehaviour
    {
        [Header("Persona Json")]
        [SerializeField] private TextAsset personaBaseJson;
        [SerializeField] private TextAsset relationshipRulesJson;
        [SerializeField] private TextAsset speechStyleJson;

        public PersonaBaseProfile PersonaBase { get; private set; }
        public RelationshipRulesProfile RelationshipRules { get; private set; }
        public SpeechStyleProfile SpeechStyle { get; private set; }

        private void Awake()
        {
            Load();
        }

        public void Load()
        {
            PersonaBase = LoadJson<PersonaBaseProfile>(personaBaseJson);
            RelationshipRules = LoadJson<RelationshipRulesProfile>(relationshipRulesJson);
            SpeechStyle = LoadJson<SpeechStyleProfile>(speechStyleJson);
        }

        public string BuildCloudActionPersonaPrompt()
        {
            return BuildPersonaJsonPrompt(
                "これはCloud Action判断に使う人格・関係性・話し方の定義です。"
            );
        }

        public string BuildCloudSpeechPersonaPrompt()
        {
            return BuildPersonaJsonPrompt(
                "これはCloud Speech生成に使う人格・関係性・話し方の定義です。"
            );
        }

        private string BuildPersonaJsonPrompt(string header)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(header);
            sb.AppendLine("以下のJSON定義を人格・関係性・話し方の基準として扱ってください。");
            sb.AppendLine("JSONの内容を説明せず、生成結果にだけ反映してください。");

            AppendJsonBlock(sb, "persona_base", personaBaseJson);
            AppendJsonBlock(sb, "relationship_rules", relationshipRulesJson);
            AppendJsonBlock(sb, "speech_style", speechStyleJson);

            return sb.ToString().Trim();
        }

        private static void AppendJsonBlock(StringBuilder sb, string label, TextAsset asset)
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            sb.AppendLine();
            sb.AppendLine("[" + label + "]");
            sb.AppendLine(asset.text.Trim());
        }

        private static T LoadJson<T>(TextAsset asset) where T : class
        {
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return null;

            try
            {
                return JsonUtility.FromJson<T>(asset.text);
            }
            catch
            {
                Debug.LogWarning("[PersonaPromptLoader] Json parse failed: " + asset.name);
                return null;
            }
        }
    }
}