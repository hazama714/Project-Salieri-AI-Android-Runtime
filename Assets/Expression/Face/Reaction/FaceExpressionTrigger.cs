using UnityEngine;
using SalieriAI.Core.Perception.Buffer;

public sealed class FaceExpressionTrigger : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FacePerceptionBuffer faceBuffer;
    [SerializeField] private ExpressionStateController expressionStateController;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void Update()
    {
        if (faceBuffer == null || expressionStateController == null)
            return;

        if (faceBuffer.BecameStableFound)
        {
            if (debugLog)
                Debug.Log($"[FaceExpressionTrigger] Stable Face Found time:{Time.time:F2}");

            if (faceBuffer.PreviousState == FacePerceptionState.TemporaryLost)
            {
                if (debugLog)
                    Debug.Log("[FaceExpressionTrigger] Recovered from TemporaryLost. Keep expression.");

                return;
            }

            expressionStateController.OnFaceStableFound();
            return;
        }

        if (faceBuffer.BecameTemporaryLost)
        {
            if (debugLog)
                Debug.Log($"[FaceExpressionTrigger] Temporary Face Lost time:{Time.time:F2}");

            expressionStateController.OnFaceTemporaryLost();
            return;
        }

        if (faceBuffer.BecameFullyLost)
        {
            if (debugLog)
                Debug.Log($"[FaceExpressionTrigger] Fully Face Lost time:{Time.time:F2}");

            expressionStateController.OnFaceFullyLost();
        }
    }
}