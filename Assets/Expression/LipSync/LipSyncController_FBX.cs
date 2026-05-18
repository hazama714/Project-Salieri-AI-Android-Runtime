// LipSyncController_FBX.cs
// バージョン: v1.0.1
// 更新日: 2025-06-11
// コメント: FBXモデル向けリップシンクコントローラー。ILipSyncControllerのインターフェースを満たすためStart/StopBlinkingをpublicに修正。

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LipSyncController_FBX : MonoBehaviour, ILipSyncController
{
    [Header("設定")]
    public SkinnedMeshRenderer faceRenderer;
    public string mouthOpenBlendShape = "MouthOpen";
    public List<NamedBlendShape> emotionBlendShapes = new();

    private int mouthBlendShapeIndex = -1;
    private Dictionary<string, int> emotionIndexMap = new();
    private Coroutine currentRoutine;

    [System.Serializable]
    public class NamedBlendShape
    {
        public string emotion;
        public string blendShapeName;
    }

    private void Awake()
    {
        if (faceRenderer == null)
        {
            faceRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        if (faceRenderer != null)
        {
            mouthBlendShapeIndex = faceRenderer.sharedMesh.GetBlendShapeIndex(mouthOpenBlendShape);

            foreach (var pair in emotionBlendShapes)
            {
                int idx = faceRenderer.sharedMesh.GetBlendShapeIndex(pair.blendShapeName);
                if (idx >= 0)
                {
                    emotionIndexMap[pair.emotion.ToLower()] = idx;
                }
            }

            Debug.Log("[LipSync_FBX] BlendShapeインデックス初期化完了");
        }
        else
        {
            Debug.LogWarning("[LipSync_FBX] SkinnedMeshRenderer が見つかりません");
        }
    }

    public void PlayLipSyncWithEmotion(string emotion, string text, float duration, AudioClip clip = null)
    {
        Debug.Log($"[LipSync_FBX] 呼び出し: 感情={emotion}, duration={duration}, clip={(clip != null ? clip.length : 0f)}");

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(LipSyncRoutine(emotion.ToLower(), duration));
    }

    private IEnumerator LipSyncRoutine(string emotion, float duration)
    {
        StopBlinking();

        ResetAllBlendShapes();

        if (emotionIndexMap.TryGetValue(emotion, out int emotionIdx))
        {
            faceRenderer.SetBlendShapeWeight(emotionIdx, 100f);
            Debug.Log($"[LipSync_FBX] 感情表情適用: {emotion}");
        }

        float time = 0f;
        while (time < duration)
        {
            float weight = Mathf.Sin(time * Mathf.PI * 2f * 2f) * 50f + 50f; // 口パクの開閉波形
            if (mouthBlendShapeIndex >= 0)
            {
                faceRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, weight);
            }

            time += Time.deltaTime;
            yield return null;
        }

        ResetAllBlendShapes();
        StartBlinking();

        Debug.Log("[LipSync_FBX] リップシンク終了・表情リセット・ブリンク再開");
    }

    public void SetEmotionExpression(string emotion)
    {
        ResetAllBlendShapes();

        if (emotionIndexMap.TryGetValue(emotion.ToLower(), out int idx))
        {
            faceRenderer.SetBlendShapeWeight(idx, 100f);
            Debug.Log($"[LipSync_FBX] 感情表情適用(SetExpression): {emotion}");
        }
    }

    public void ResetAllExpressions()
    {
        ResetAllBlendShapes();
    }

    private void ResetAllBlendShapes()
    {
        if (faceRenderer == null) return;

        int count = faceRenderer.sharedMesh.blendShapeCount;
        for (int i = 0; i < count; i++)
        {
            faceRenderer.SetBlendShapeWeight(i, 0f);
        }

        Debug.Log("[LipSync_FBX] 全表情リセット完了");
    }

    public void StopBlinking()
    {
        Debug.Log("[LipSync_FBX] ブリンク停止（未実装）");
    }

    public void StartBlinking()
    {
        Debug.Log("[LipSync_FBX] ブリンク開始（未実装）");
    }
}
