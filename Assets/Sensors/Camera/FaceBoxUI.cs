using UnityEngine;

public class FaceBoxUI : MonoBehaviour
{
    [Header("Input")]
    public FaceDetector_OpenCV detector;

    [Header("UI")]
    public RectTransform box;
    public RectTransform wipeRect;

    [Header("Mirror")]
    public bool mirrorX = false;
    public bool mirrorY = false;

    private void Update()
    {
        if (detector == null || box == null || wipeRect == null)
            return;

        if (!detector.HasFace)
        {
            box.gameObject.SetActive(false);
            return;
        }

        float frameW = detector.FrameWidth;
        float frameH = detector.FrameHeight;

        if (frameW <= 0f || frameH <= 0f)
        {
            box.gameObject.SetActive(false);
            return;
        }

        box.gameObject.SetActive(true);

        float x = detector.FaceCenterX / frameW;
        float y = detector.FaceCenterY / frameH;
        float w = detector.FaceWidth / frameW;
        float h = detector.FaceHeight / frameH;

        if (mirrorX)
            x = 1f - x;

        if (mirrorY)
            y = 1f - y;

        float wipeW = wipeRect.rect.width;
        float wipeH = wipeRect.rect.height;

        box.anchoredPosition = new Vector2(
            (x - 0.5f) * wipeW,
            (0.5f - y) * wipeH
        );

        box.sizeDelta = new Vector2(
            w * wipeW,
            h * wipeH
        );
    }
}