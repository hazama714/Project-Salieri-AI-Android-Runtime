using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using VRM;

[Preserve]
public class VRM0ExpressionController : MonoBehaviour, IExpressionController
{
    [SerializeField]
    public VRMBlendShapeProxy blendShapeProxy;

    public string modelName = "default";

    private readonly Dictionary<BlendShapeKey, float> currentValues =
        new Dictionary<BlendShapeKey, float>();

    [Preserve]
    public void SetExpression(string emotion)
    {
        SetExpressionWeight(emotion, 1.0f);
    }

    public void SetExpressionWeight(string emotion, float weight)
    {
        if (blendShapeProxy == null)
        {
            Debug.LogWarning("[VRMExpressionController] blendShapeProxy ñ¢ê›íË");
            return;
        }

        if (string.IsNullOrEmpty(emotion))
            emotion = "Neutral";

        weight = Mathf.Clamp01(weight);

        BlendShapePreset preset = MapEmotionToPreset(emotion);
        BlendShapeKey key = BlendShapeKey.CreateFromPreset(preset);

        currentValues[key] = weight;

        Debug.Log(
            $"[VRMExpressionController] SetExpressionWeight emotion:{emotion} preset:{preset} weight:{weight}"
        );
    }

    public void ResetExpression()
    {
        currentValues.Clear();
    }

    private void LateUpdate()
    {
        if (blendShapeProxy == null)
            return;

        blendShapeProxy.SetValues(currentValues);
    }

    private BlendShapePreset MapEmotionToPreset(string emotion)
    {
        string lower = emotion.ToLower();

        if (lower.Contains("joy"))
            return BlendShapePreset.Joy;

        if (lower.Contains("angry"))
            return BlendShapePreset.Angry;

        if (lower.Contains("sorrow"))
            return BlendShapePreset.Sorrow;

        if (lower.Contains("fun"))
            return BlendShapePreset.Fun;

        if (lower.Contains("neutral"))
            return BlendShapePreset.Neutral;

        return BlendShapePreset.Neutral;
    }
}