using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("--References--")]
    public GameObject player;
    public PlayerController playerController;
    public PlayerWall playerWall;
    public WeaponSlot weaponSlot;
    public AbilitySlot abilitySlot;

[Header("--States--")]
    public bool isPaused;
    public bool isWallDestroyed;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        player = GameObject.FindWithTag("Player");
        playerController = player.GetComponent<PlayerController>();
        weaponSlot = player.GetComponent<WeaponSlot>();
        playerWall = GameObject.FindWithTag("PlayerWall").GetComponent<PlayerWall>();
        abilitySlot = player.GetComponent<AbilitySlot>();
        SyncEquippedAbilities();
        SyncEquippedWeapons();
    }
    void Start()
    {

    }

    void Update()
    {
        
    }

    private void SyncEquippedWeapons()
    {
        if (PlayerInventory.Instance != null && weaponSlot != null)
        {
            for (int i = 0; i < PlayerInventory.Instance.EquippedWeapons.Length; i++)
            {
                WeaponData weapon = PlayerInventory.Instance.EquippedWeapons[i];
                if (weapon != null)
                    weaponSlot.AddWeaponToSlot(weapon, i);
            }
        }
        else
        {
            Debug.LogError("PlayerInventory or WeaponSlot is null in GameManager");
        }
    }

    private void SyncEquippedAbilities()
    {
        if (PlayerInventory.Instance != null && abilitySlot != null)
        {
            for (int i = 0; i < PlayerInventory.Instance.equippedAbilities.Length; i++)
            {
                Ability ability = PlayerInventory.Instance.equippedAbilities[i];
                if (ability != null)
                {
                    // Create a new instance of the ability
                    Ability newAbility = Instantiate(ability);
                    abilitySlot.EquipAbility(newAbility, i);
                }
            }

            foreach (var passiveAbility in PlayerInventory.Instance.passiveAbilities)
            {
                if (passiveAbility != null)
                {
                    Ability newPassiveAbility = Instantiate(passiveAbility);
                    abilitySlot.AddPassiveAbility(newPassiveAbility);
                }
            }
        }
        else
        {
            Debug.Log("PlayerInventory or AbilitySlot is null in GameManager");
        }
    }

    public void statePaused()
    {
        isPaused = !isPaused;
        Time.timeScale = 0;
        UIManager.Instance.ShowPauseMenu();
    }
    public void stateUnpaused()
    {
        isPaused = !isPaused;
        Time.timeScale = 1;
        UIManager.Instance.HideActiveMenu();
    }

    public void WinGame()
    {
        isPaused = true;
        Time.timeScale = 0;

        var (xpReward, currencyReward) = LevelManager.Instance.GetLevelRewards();
        PlayerInventory.Instance.AddLevelCompletionReward(xpReward, currencyReward);

        UIManager.Instance.ShowWinMenu();
    }

    public void LoseGame()
    {
        isPaused = true;
        Time.timeScale = 0;
        UIManager.Instance.ShowLoseMenu();
    }
}
