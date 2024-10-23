using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : MonoBehaviour
{
    public RectTransform startPoint;
    public RectTransform endPoint;
    private RectTransform rectTransform;
    private Image lineImage;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
    }

    public void SetPoints(RectTransform start, RectTransform end)
    {
        startPoint = start;
        endPoint = end;
        UpdateLine();
    }

    public void UpdateLine()
    {
        if (startPoint == null || endPoint == null) return;

        Vector2 direction = endPoint.anchoredPosition - startPoint.anchoredPosition;
        float distance = direction.magnitude;

        rectTransform.anchoredPosition = startPoint.anchoredPosition;
        rectTransform.sizeDelta = new Vector2(distance, 1f); // Line thickness of 2 pixels

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}