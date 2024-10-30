using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerNameSettings : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TextMeshProUGUI errorMessageText;
    [SerializeField] private Button confirmButton;

    [Header("Validation Settings")]
    [SerializeField] private int minNameLength = 3;
    [SerializeField] private int maxNameLength = 20;

    private const string PLAYER_NAME_KEY = "PlayerName";
    private Color defaultInputColor;

    private void Start()
    {
        InitializeUI();
        LoadSavedName();
        SetupInputValidation();
    }

    private void InitializeUI()
    {
        if (errorMessageText != null)
        {
            errorMessageText.gameObject.SetActive(false);
        }

        if (nameInputField != null)
        {
            defaultInputColor = nameInputField.textComponent.color;
        }
        else
        {
            Debug.LogError("Name Input Field not assigned in PlayerNameSettings!");
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(SavePlayerName);
            confirmButton.interactable = false;
        }
    }

    private void LoadSavedName()
    {
        string savedName = PlayerPrefs.GetString(PLAYER_NAME_KEY, "");
        if (!string.IsNullOrEmpty(savedName))
        {
            nameInputField.text = savedName;
            ValidateNameInput(savedName); // Validate the loaded name
        }
    }

    private void SetupInputValidation()
    {
        nameInputField.onValueChanged.AddListener(ValidateNameInput);
    }

    private void ValidateNameInput(string input)
    {
        bool isValid = true;
        string errorMessage = "";

        // Check for empty or whitespace
        if (string.IsNullOrWhiteSpace(input))
        {
            isValid = false;
            errorMessage = "Name cannot be empty";
        }
        // Check minimum length
        else if (input.Length < minNameLength)
        {
            isValid = false;
            errorMessage = $"Name must be at least {minNameLength} characters";
        }
        // Check maximum length
        else if (input.Length > maxNameLength)
        {
            isValid = false;
            errorMessage = $"Name cannot exceed {maxNameLength} characters";
        }

        // Update UI based on validation
        UpdateUIValidationState(isValid, errorMessage);
    }

    private void UpdateUIValidationState(bool isValid, string errorMessage)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = errorMessage;
            errorMessageText.gameObject.SetActive(!isValid);
        }

        if (nameInputField != null)
        {
            nameInputField.textComponent.color = isValid ? defaultInputColor : Color.red;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = isValid;
        }
    }

    private void SavePlayerName()
    {
        string playerName = nameInputField.text.Trim();

        if (ValidateName(playerName))
        {
            // Save to PlayerPrefs
            PlayerPrefs.SetString(PLAYER_NAME_KEY, playerName);
            PlayerPrefs.Save();

            // Update PlayerInventory
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.SetPlayerName(playerName);
            }

            // Update MainMenuPlayer display if it exists
            MainMenuPlayer mainMenuPlayer = FindObjectOfType<MainMenuPlayer>();
            if (mainMenuPlayer != null)
            {
                mainMenuPlayer.UpdatePlayerNameDisplay();
            }

            Debug.Log($"Player name saved: {playerName}");
        }
    }

    private bool ValidateName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) &&
               name.Length >= minNameLength &&
               name.Length <= maxNameLength;
    }
}
