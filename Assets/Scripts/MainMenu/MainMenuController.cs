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
        public List<SubMenuInfo> subMenus = new List<SubMenuInfo>();
    }

    [System.Serializable]
    public class SubMenuInfo
    {
        public string name;
        public GameObject menuObject;
        [Tooltip("If true, will hide other sub-menus when this is shown")]
        public bool exclusiveDisplay = true;
    }

    public List<MenuInfo> menus = new List<MenuInfo>();
    private MenuInfo activeMenu;

    [Header("Menu References")]
    public GameObject gameMenu;
    public GameObject gameModeMenu;
    public GameObject storyModeMenu;
    public GameObject DCODEGloveMenu;
    public GameObject settingsMenu;

    [Header("DCODE Glove Sub-Menus")]
    public GameObject weaponsTab;
    public GameObject skillTreeTab;

    private void Start()
    {
        InitializeMenus();
        SetActiveMenu("GameMenu");
        AudioManager.Instance.PlayMusic("MainMenuMusic");
    }

    private void InitializeMenus()
    {
        var mainMenu = new MenuInfo { name = "GameMenu", menuObject = gameMenu };
        var gameMode = new MenuInfo { name = "GameModeMenu", menuObject = this.gameModeMenu };
        var story = new MenuInfo { name = "StoryModeMenu", menuObject = storyModeMenu };
        var dcodeGlove = new MenuInfo { name = "DCODEGloveMenu", menuObject = DCODEGloveMenu };
        var settings = new MenuInfo { name = "SettingsMenu", menuObject = settingsMenu };

        dcodeGlove.subMenus.Add(new SubMenuInfo
        {
            name = "WeaponsTab",
            menuObject = weaponsTab,
            exclusiveDisplay = true
        });
        dcodeGlove.subMenus.Add(new SubMenuInfo
        {
            name = "SkillTreeTab",
            menuObject = skillTreeTab,
            exclusiveDisplay = true
        });

        menus.Add(mainMenu);
        menus.Add(gameMode);
        menus.Add(story);
        menus.Add(dcodeGlove);
        menus.Add(settings);
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

            if (menuToActivate.subMenus.Count > 0)
            {
                foreach (var subMenu in menuToActivate.subMenus)
                {
                    subMenu.menuObject.SetActive(subMenu == menuToActivate.subMenus[0]);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Menu '{menuName}' not found.");
        }
    }

    public void ToggleSubMenu(string subMenuName)
    {
        if (activeMenu == null)
            return;

        SubMenuInfo subMenu = activeMenu.subMenus.Find(sm => sm.name == subMenuName);
        if (subMenu != null)
        {
            if (subMenu.exclusiveDisplay)
            {
                // Deactivate all other sub-menus in this menu
                foreach (var otherSubMenu in activeMenu.subMenus)
                {
                    otherSubMenu.menuObject.SetActive(otherSubMenu == subMenu);
                }
            }
            else
            {
                // Just toggle this sub-menu
                subMenu.menuObject.SetActive(!subMenu.menuObject.activeSelf);
            }
        }
        else
        {
            Debug.LogWarning($"Sub-menu '{subMenuName}' not found in active menu '{activeMenu.name}'.");
        }
    }

    // Helper method to check if a sub-menu is active
    public bool IsSubMenuActive(string subMenuName)
    {
        if (activeMenu == null)
            return false;

        SubMenuInfo subMenu = activeMenu.subMenus.Find(sm => sm.name == subMenuName);
        return subMenu != null && subMenu.menuObject.activeSelf;
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

    public void OpenGamemodeMenu()
    {
        SetActiveMenu("GameModeMenu");
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