using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public class MenuInfo
    {
        public string name;
        public GameObject menuObject;
    }

    public List<MenuInfo> menus = new List<MenuInfo>();
    private MenuInfo activeMenu;

    [Header("Menu References")]
    public GameObject gameMenu;
    public GameObject levelSelectMenu;
    public GameObject weaponShopMenu;
    public GameObject skillTreeMenu;
    public GameObject settingsMenu;

    private void Start()
    {
        InitializeMenus();
        // Set the initial active menu (e.g., the main game menu)
        SetActiveMenu("GameMenu");
    }

    private void InitializeMenus()
    {
        menus.Add(new MenuInfo { name = "GameMenu", menuObject = gameMenu });
        menus.Add(new MenuInfo { name = "LevelSelectMenu", menuObject = levelSelectMenu });
        menus.Add(new MenuInfo { name = "WeaponShopMenu", menuObject = weaponShopMenu });
        menus.Add(new MenuInfo { name = "SkillTreeMenu", menuObject = skillTreeMenu });
        menus.Add(new MenuInfo { name = "SettingsMenu", menuObject = settingsMenu });
    }

    public void ToggleMenu(string menuName)
    {
        MenuInfo menuToToggle = menus.Find(m => m.name == menuName);

        if (menuToToggle != null)
        {
            if (activeMenu != menuToToggle)
            {
                // Deactivate the current active menu
                if (activeMenu != null)
                {
                    activeMenu.menuObject.SetActive(false);
                }

                // Activate the new menu
                menuToToggle.menuObject.SetActive(true);
                activeMenu = menuToToggle;
            }
            else
            {
                // If it's already the active menu, just toggle it
                activeMenu.menuObject.SetActive(!activeMenu.menuObject.activeSelf);
            }
        }
        else
        {
            Debug.LogWarning($"Menu '{menuName}' not found.");
        }
    }

    public void SetActiveMenu(string menuName)
    {
        MenuInfo menuToActivate = menus.Find(m => m.name == menuName);

        if (menuToActivate != null)
        {
            foreach (var menu in menus)
            {
                menu.menuObject.SetActive(false);
            }

            menuToActivate.menuObject.SetActive(true);
            activeMenu = menuToActivate;
        }
        else
        {
            Debug.LogWarning($"Menu '{menuName}' not found.");
        }
    }

    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene(levelIndex);
    }

    public void ExitGame()
    {
        // Reset the player's inventory when exiting the game
        PlayerInventory.Instance.ResetInventory();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    public void OpenWeaponShop()
    {
        SetActiveMenu("WeaponShopMenu");
    }

    public void OpenUpgradesMenu()
    {
        SetActiveMenu("UpgradesMenu");
    }

    public void OpenSettingsMenu()
    {
        SetActiveMenu("SettingsMenu");
    }

    public void ReturnToMainMenu()
    {
        SetActiveMenu("GameMenu");
    }

    public void OpenLevelSelectMenu()
    {
        SetActiveMenu("LevelSelectMenu");
    }

    public void ToggleMenuVisibility(string menuName)
    {
        MenuInfo menu = menus.Find(m => m.name == menuName);
        if (menu != null)
        {
            menu.menuObject.SetActive(!menu.menuObject.activeSelf);
        }
        else
        {
            Debug.LogWarning($"Menu '{menuName}' not found.");
        }
    }

    public bool IsMenuActive(string menuName)
    {
        MenuInfo menu = menus.Find(m => m.name == menuName);
        return menu != null && menu.menuObject.activeSelf;
    }
}