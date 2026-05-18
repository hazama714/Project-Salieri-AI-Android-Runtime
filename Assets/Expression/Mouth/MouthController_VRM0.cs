using UnityEngine;
using VRM;

public sealed class MouthController_VRM0 : MonoBehaviour, IMouthController
{
    [SerializeField]
    private VRMBlendShapeProxy blendShapeProxy;

    [Header("Mouth")]
    [SerializeField]
    private BlendShapePreset mouthPreset = BlendShapePreset.A;

    public void SetMouthOpen(float value)
    {
        if (blendShapeProxy == null)
            return;

        value = Mathf.Clamp01(value);

        BlendShapeKey key = BlendShapeKey.CreateFromPreset(mouthPreset);

        blendShapeProxy.ImmediatelySetValue(key, value);
    }

    public void CloseMouth()
    {
        SetMouthOpen(0f);
    }
}