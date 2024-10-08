using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{

    public static UIManager Instance;

    [SerializeField] GameObject menuPause;
    [SerializeField] GameObject menuWin;
    [SerializeField] GameObject menuLose;
    [SerializeField] private TextMeshProUGUI waveText;

    private GameObject activeMenu;

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
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
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

    public void UpdateWaveDisplay(string waveInfo)
    {
        waveText.text = waveInfo;
        waveText.gameObject.SetActive(true);
    }

    public void HideWaveDisplay()
    {
        waveText.gameObject.SetActive(false); 
    }

    public void ShowPauseMenu()
    {
        SetActiveMenu(menuPause);
    }

    public void ShowWinMenu()
    {
        SetActiveMenu(menuWin);
    }

    public void ShowLoseMenu()
    {
        SetActiveMenu(menuLose);
    }

    public void HideActiveMenu()
    {
        if (activeMenu != null)
        {
            activeMenu.SetActive(false);
            activeMenu = null;
        }
    }

    private void SetActiveMenu(GameObject menu)
    {
        HideActiveMenu();
        activeMenu = menu;
        activeMenu.SetActive(true);
    }
}
