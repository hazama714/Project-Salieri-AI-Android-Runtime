// FBXExpressionController.cs
// バージョン: v1.2.0
// 更新日: 2025-06-11
// コメント: FBXモデルにおける表情制御（BlendShape）を感情に応じて適用（IExpressionController準拠 + JSON読み込み）
// - IExpressionController実装（SetExpression/ResetExpression）に対応
// - Resources/ExpressionMappings/expression_mapping_{modelName}.json を読み込み

using System.Collections.Generic;
using UnityEngine;
using System;

public class FBXExpressionController : MonoBehaviour, IExpressionController
{
    [Header("対象メッシュ")]
    public SkinnedMeshRenderer targetRenderer;

    [Header("モデル識別名（例: necomaid）")]
    public string modelName = "necomaid";

    private Dictionary<string, string> emotionMap = new();

    void Start()
    {
        LoadExpressionMapping(modelName);
    }

    public void SetExpression(string emotion)
    {
        if (targetRenderer == null || targetRenderer.sharedMesh == null)
        {
            Debug.LogWarning("[FBXExpressionController] targetRendererが未設定です。");
            return;
        }

        var mesh = targetRenderer.sharedMesh;
        int blendShapeCount = mesh.blendShapeCount;

        // 全BlendShapeリセット
        for (int i = 0; i < blendShapeCount; i++)
        {
            targetRenderer.SetBlendShapeWeight(i, 0f);
        }

        // 対応モーフ名を取得
        if (emotionMap.TryGetValue(emotion.ToLower(), out string blendShapeName))
        {
            for (int i = 0; i < blendShapeCount; i++)
            {
                if (mesh.GetBlendShapeName(i) == blendShapeName)
                {
                    targetRenderer.SetBlendShapeWeight(i, 100f);
                    Debug.Log($"[FBXExpressionController] {emotion} → {blendShapeName}");
                    return;
                }
            }
            Debug.LogWarning($"[FBXExpressionController] モーフ '{blendShapeName}' が見つかりませんでした。");
        }
        else
        {
            Debug.LogWarning($"[FBXExpressionController] 感情 '{emotion}' に対するモーフが定義されていません。");
        }
    }

    public void ResetExpression()
    {
        if (targetRenderer == null || targetRenderer.sharedMesh == null) return;

        for (int i = 0; i < targetRenderer.sharedMesh.blendShapeCount; i++)
        {
            targetRenderer.SetBlendShapeWeight(i, 0f);
        }
        Debug.Log("[FBXExpressionController] 全BlendShapeをリセットしました。");
    }

    private void LoadExpressionMapping(string name)
    {
        string resourcePath = $"ExpressionMappings/expression_mapping_{name.ToLower()}";
        TextAsset jsonFile = Resources.Load<TextAsset>(resourcePath);

        if (jsonFile == null)
        {
            Debug.LogWarning($"[FBXExpressionController] ResourcesからJSONが読み込めませんでした: {resourcePath}");
            return;
        }

        try
        {
            ExpressionMapping loaded = JsonUtility.FromJson<ExpressionMapping>(jsonFile.text);
            if (loaded != null && loaded.emotionMap != null)
            {
                emotionMap = loaded.emotionMap;
                Debug.Log($"[FBXExpressionController] JSONマッピング読み込み成功: {resourcePath}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FBXExpressionController] JSON読み込み失敗: {e.Message}");
        }
    }

    [Serializable]
    public class ExpressionMapping
    {
        public Dictionary<string, string> emotionMap;
        public Dictionary<string, string> lipSyncMap; // 現在は未使用
    }
}
