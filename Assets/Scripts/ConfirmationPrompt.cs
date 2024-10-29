using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ConfirmationPrompt : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action onConfirm;
    private System.Action onCancel;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public void Show(string message, System.Action confirmAction, System.Action cancelAction)
    {
        promptText.text = message;
        onConfirm = confirmAction;
        onCancel = cancelAction;
        gameObject.SetActive(true);
    }

    private void OnConfirmClicked()
    {
        onConfirm?.Invoke();
        Hide();
    }

    private void OnCancelClicked()
    {
        onCancel?.Invoke();
        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
