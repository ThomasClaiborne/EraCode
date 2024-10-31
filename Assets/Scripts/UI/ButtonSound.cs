using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public string hoverSound = "ButtonHover";
    public string clickSound = "ButtonClick";
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable && AudioManager.Instance != null && hoverSound != "")
        {
            AudioManager.Instance.PlaySFX(hoverSound);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button.interactable && AudioManager.Instance != null && clickSound != "")
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }
    }
}
