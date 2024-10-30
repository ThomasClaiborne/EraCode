using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Pause Menu")]
    [SerializeField] GameObject menuPause;

    [Header("Win Menu")]
    [SerializeField] GameObject menuWin;

    [Header("Lose Menu")]
    [SerializeField] GameObject menuLose;

    [Header("Message System")]
    [SerializeField] private GameObject messageEventPrefab;
    [SerializeField] private Transform messagePoint;
    [SerializeField] private float messageDuration = 0.5f;

    [Header("Mission Status")]
    [SerializeField] private GameObject missionStatusObject;
    [SerializeField] private TextMeshProUGUI missionStatusLabel;
    [SerializeField] private TextMeshProUGUI missionNameLabel;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private Animator missionStatusAnimator;
    [SerializeField] private Color winColor = new Color(0, 1, 0, 1);
    [SerializeField] private Color loseColor = new Color(1, 0, 0, 1); 

    [Header("Mission Result")]
    [SerializeField] private GameObject missionResultObject;
    [SerializeField] private Image missionResultBackground;
    [SerializeField] private TextMeshProUGUI playerKillCountText;
    [SerializeField] private TextMeshProUGUI synthiumCountText;
    [SerializeField] private Animator missionResultAnimator;

    private GameObject activeMenu;
    private Coroutine deactivateCoroutine;
    private GameObject currentMissionStatus;
    private bool canPause;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void Start()
    {
        canPause = true;

        if (missionStatusObject != null)
            missionStatusObject.SetActive(false);
        if (missionResultObject != null)
            missionResultObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && canPause)
        {
            if (GameManager.Instance.isPaused)
            {
                GameManager.Instance.stateUnpaused();
            }
            else
            {
                GameManager.Instance.statePaused();
            }
        }
    }

    public void CanPause(bool input)
    {
        canPause = input;
    }

    public void ShowMessage(string message, Color messageColor, float duration)
    {
        StartCoroutine(HandleMessageDisplay(message, messageColor, duration));
    }

    private IEnumerator HandleMessageDisplay(string message, Color messageColor, float duration)
    {
        GameObject messageObj = Instantiate(messageEventPrefab, messagePoint);

        TextMeshProUGUI messageText = messageObj.GetComponentInChildren<TextMeshProUGUI>();
        Animator animator = messageObj.GetComponent<Animator>();

        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = messageColor;
        }
        else
        {
            Debug.LogError("TextMeshProUGUI component not found in message prefab!");
        }

        yield return new WaitForSeconds(duration);

        if (animator != null)
        {
            animator.SetBool("Active", false);

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float outroLength = stateInfo.length;

            yield return new WaitForSeconds(outroLength + messageDuration);
            Destroy(messageObj);
        }
        else
        {
            Debug.LogError("Animator component not found in message prefab!");
            Destroy(messageObj);
        }
    }

    public void UpdateWaveDisplay(string message, Color color)
    {
        ShowMessage(message, color, 3f);
    }

    public void ShowMissionStatus(bool isWin)
    {
        string levelName = LevelManager.Instance.LevelName;
        string status = isWin ? "MISSION COMPLETE" : "MISSION FAILED";
        Color statusColor = isWin ? winColor : loseColor;

        CreateMissionStatus(statusColor, status, levelName);
    }

    private void CreateMissionStatus(Color color, string status, string missionName)
    {
        if (missionStatusLabel != null)
            missionStatusLabel.text = status;

        if (missionNameLabel != null)
            missionNameLabel.text = missionName;

        if (backgroundImage != null)
            backgroundImage.color = color;

        if (vignetteImage != null)
            vignetteImage.color = color;

        if (glowImage != null)
            glowImage.color = color;

        missionStatusObject.SetActive(true);
        if (missionStatusAnimator != null)
        {
            missionStatusAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            missionStatusAnimator.SetBool("Active", true);
        }

    }

    public void HideMissionStatus()
    {
        if (missionStatusObject != null && missionStatusObject.activeSelf)
        {
            if (missionStatusAnimator != null)
            {
                missionStatusAnimator.SetBool("Active", false);
                continueButton.SetActive(false);
                StartCoroutine(DeactivateMissionStatus());
            }
            else
            {
                missionStatusObject.SetActive(false);
            }
        }
    }
    private IEnumerator DeactivateMissionStatus()
    {
        yield return new WaitForSecondsRealtime(1f); // Wait for out animation
        missionStatusObject.SetActive(false);
    }

    public void ShowPauseMenu()
    {
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }

        SetActiveMenu(menuPause);

        if (activeMenu != null)
        {
            Animator animator = activeMenu.GetComponent<Animator>();
            if (animator != null)
            {
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.SetBool("Active", true);
            }
        }
    }

    public void ShowWinMenu()
    {
        CanPause(false);
        ShowMissionStatus(true);
    }

    public void ShowLoseMenu()
    {
        CanPause(false);
        ShowMissionStatus(false);
    }

    public void OnContinueButtonPressed()
    {
        HideMissionStatus();
        StartCoroutine(ShowMissionResultDelayed());
    }

    private IEnumerator ShowMissionResultDelayed()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        ShowMissionResult();
    }

    private void ShowMissionResult()
    {
        // Get stats from LevelManager
        int kills = LevelManager.Instance.PlayerKills;
        int synthium = LevelManager.Instance.SynthiumEarned;
        bool isWin = !GameManager.Instance.isWallDestroyed;

        // Set up the UI
        if (missionResultBackground != null)
            missionResultBackground.color = isWin ? winColor : loseColor;

        if (playerKillCountText != null)
            playerKillCountText.text = kills.ToString();

        if (synthiumCountText != null)
            synthiumCountText.text = synthium.ToString();

        // Show and animate
        missionResultObject.SetActive(true);
        if (missionResultAnimator != null)
        {
            missionResultAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            missionResultAnimator.SetBool("Active", true);
        }
    }

    public void HideActiveMenu()
    {
        if (activeMenu != null)
        {
            Animator animator = activeMenu.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("Active", false);
                deactivateCoroutine = StartCoroutine(DeactivateMenu(activeMenu));
            }
            else
            {
                activeMenu.SetActive(false);
                activeMenu = null;
            }
        }
    }

    private IEnumerator DeactivateMenu(GameObject menu)
    {
        yield return new WaitForSeconds(1f);
        if (menu == activeMenu)
        {
            menu.SetActive(false);
            activeMenu = null;
        }

        deactivateCoroutine = null;
    }

    private void SetActiveMenu(GameObject menu)
    {
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }

        if (activeMenu == menu)
            return;

        if (activeMenu != null)
        {
            Animator currentAnimator = activeMenu.GetComponent<Animator>();
            if (currentAnimator != null)
            {
                currentAnimator.SetBool("Active", false);
                deactivateCoroutine = StartCoroutine(DeactivateMenu(activeMenu));

            }
            else
            {
                activeMenu.SetActive(false);
            }
        }

        activeMenu = menu;
        activeMenu.SetActive(true);
        Animator newAnimator = activeMenu.GetComponent<Animator>();
        if (newAnimator != null)
        {
            newAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            newAnimator.SetBool("Active", true);
        }
    }
}
