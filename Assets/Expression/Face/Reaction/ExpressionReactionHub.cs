using System.Collections;
using UnityEngine;

public sealed class ExpressionReactionHub : MonoBehaviour
{
    [Header("Expression Controller")]
    [SerializeField] private MonoBehaviour expressionControllerBehaviour;

    [Header("Face Found Reaction")]
    [SerializeField] private string faceFoundExpressionName = "Joy";
    [SerializeField] private string neutralExpressionName = "Neutral";

    [Header("Timing")]
    [SerializeField] private float fadeInSeconds = 0.35f;
    [SerializeField] private float holdSeconds = 0.8f;
    [SerializeField] private float fadeOutSeconds = 0.8f;
    [SerializeField] private float maxStrength = 0.6f;

    private IExpressionController expressionController;
    private VRM0ExpressionController vrmExpressionController;
    private Coroutine reactionCoroutine;

    private void Awake()
    {
        expressionController = expressionControllerBehaviour as IExpressionController;
        vrmExpressionController = expressionControllerBehaviour as VRM0ExpressionController;

        if (expressionController == null)
        {
            Debug.LogWarning(
                "[ExpressionReactionHub] expressionControllerBehaviour does not implement IExpressionController."
            );
        }
    }

    public void OnFaceFound()
    {
        Debug.Log("[ExpressionReactionHub] OnFaceFound");

        if (reactionCoroutine != null)
            StopCoroutine(reactionCoroutine);

        // Neutral Çâ∫Ç∞ÇÈ
        SetExpression(neutralExpressionName, 0f);

        reactionCoroutine = StartCoroutine(FaceFoundRoutine());
    }

    public void OnFaceLost()
    {
        Debug.Log("[ExpressionReactionHub] OnFaceLost");

        if (reactionCoroutine != null)
            StopCoroutine(reactionCoroutine);

        reactionCoroutine = StartCoroutine(FaceLostRoutine());
    }

    private IEnumerator FaceLostRoutine()
    {
        yield return FadeExpression(
            faceFoundExpressionName,
            maxStrength,
            0f,
            fadeOutSeconds
        );

        SetExpression(neutralExpressionName, 1f);

        reactionCoroutine = null;
    }

    private IEnumerator FaceFoundRoutine()
    {
        yield return FadeExpression(faceFoundExpressionName, 0f, maxStrength, fadeInSeconds);

        // äÁÇ™å©Ç¶ÇƒÇ¢ÇÈä‘ÇÕï\èÓÇà€éùÇ∑ÇÈÅB
        // Ç±Ç±Ç≈ÇÕ fadeOut Ç‡ Neutral ñﬂÇµÇ‡ÇµÇ»Ç¢ÅB

        reactionCoroutine = null;
    }

    private IEnumerator FadeExpression(string expressionName, float from, float to, float seconds)
    {
        float timer = 0f;

        while (timer < seconds)
        {
            timer += Time.deltaTime;

            float t = seconds <= 0f
                ? 1f
                : Mathf.Clamp01(timer / seconds);

            float value = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));

            SetExpression(expressionName, value);

            yield return null;
        }

        SetExpression(expressionName, to);
    }

    private void SetExpression(string expressionName, float strength)
    {
        if (vrmExpressionController != null)
        {
            vrmExpressionController.SetExpressionWeight(expressionName, strength);
            Debug.Log($"[ExpressionReactionHub] SetExpressionWeight: {expressionName} {strength}");
            return;
        }

        if (expressionController == null)
        {
            Debug.LogWarning("[ExpressionReactionHub] expressionController is null");
            return;
        }

        expressionController.SetExpression(expressionName);
        Debug.Log($"[ExpressionReactionHub] SetExpression: {expressionName}");
    }
}