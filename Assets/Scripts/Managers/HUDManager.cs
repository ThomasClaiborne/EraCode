using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("--Action Bar--")]
    [SerializeField] private GameObject[] actionBarSlots = new GameObject[5];
    [SerializeField] private Animator levelUpAnimator;
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite disabledSlotSprite;

    private Image[] slotIcons;
    private Animator[] slotAnimators;

    [Header("--Wall--")]
    [SerializeField] private Slider wallHealthSlider;
    //[SerializeField] private TextMeshProUGUI wallHealthText;

    [Header("--Current Weapon--")]
    [SerializeField] private GameObject currentWeaponDisplay;
    [SerializeField] private Transform ammoListContainer;
    [SerializeField] private GameObject toggleBulletPrefab;
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] public TextMeshProUGUI ammoCountText;

    private List<Toggle> currentToggleBullets = new List<Toggle>();

    [Header("--Reload Indicator--")]
    [SerializeField] private GameObject reloadIndicatorObject;
    [SerializeField] private Slider reloadSlider;
    private Coroutine reloadCoroutine;

    [Header("--Currency--")]
    public TextMeshProUGUI currencyText;

    [Header("--XP--")]
    [SerializeField] private Slider XPBarSlider;
    public TextMeshProUGUI XPText;
    public TextMeshProUGUI LevelText;

    [Header("--Message--")]
    public TextMeshProUGUI messageText;

    public float lerpSpeed = 2f;

    private Coroutine currentCoroutine;
    private Dictionary<TextMeshProUGUI, Coroutine> lerpCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();
    private Color defaultAmmoTextColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        InitializeActionBarArrays();
    }
    void Start()
    {
        UpdateSynthiumText();
        UpdateLevelDisplay();
    }

    void Update()
    {
        
    }

    private void InitializeActionBarArrays()
    {
        slotIcons = new Image[actionBarSlots.Length];
        slotAnimators = new Animator[actionBarSlots.Length];

        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            // Get the Icon component under Mask/Item
            slotIcons[i] = actionBarSlots[i].transform
                .Find("MASK/Item/Icon").GetComponent<Image>();

            // Get the Animator component
            slotAnimators[i] = actionBarSlots[i].GetComponent<Animator>();

        }
    }

    public void PopulateActionBar(WeaponData[] equippedWeapons)
    {
        for (int i = 0; i < actionBarSlots.Length; i++)
        {
            if (i < equippedWeapons.Length && equippedWeapons[i] != null)
            {
                slotIcons[i].sprite = equippedWeapons[i].actionBarIcon;
                slotAnimators[i].SetTrigger("Normal");
            }
            else
            {
                slotIcons[i].sprite = emptySlotSprite;
                slotAnimators[i].SetTrigger("Normal");
            }
        }
    }

    public void HighlightSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotAnimators.Length)
        {
            for (int i = 0; i < slotAnimators.Length; i++)
            {
                slotAnimators[i].SetTrigger("Normal");
            }
            slotAnimators[slotIndex].SetTrigger("Pressed");
        }
    }

    public void DisableSlot(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < slotAnimators.Length)
        {
            slotAnimators[slotIndex].SetTrigger("Disabled");
            // slotIcons[slotIndex].sprite = disabledSlotSprite;
        }
    }

    public void PlayLevelUpAnimation()
    {
        if (levelUpAnimator != null)
        {
            levelUpAnimator.SetTrigger("LevelUp");
        }
    }

    public void InitializeWallHealth(int maxHealth)
    {
        wallHealthSlider.value = 1f;
        //UpdateWallHealthText(maxHealth);
    }

    public void UpdateWallHealth(int currentHealth, int maxHealth)
    {
        float normalizedHealth = (float)currentHealth / maxHealth;
        wallHealthSlider.value = normalizedHealth;
        //UpdateWallHealthText(currentHealth);
    }

    private void UpdateWallHealthText(int health)
    {
        //wallHealthText.text = health.ToString();
    }

    public void UpdateCurrentWeapon(WeaponData weaponData, int currentClipSize, int totalAmmo)
    {
        if (weaponData == null) return;

        weaponIconImage.sprite = weaponData.weaponIcon;
        weaponNameText.text = weaponData.weaponName;
        ammoCountText.text = weaponData.isInfiniteAmmo ? "\u221E" : totalAmmo.ToString();

        ClearToggleBullets();

        int clipSize = PlayerInventory.Instance.GetWeaponClipSize(weaponData.weaponId);
        HorizontalLayoutGroup layoutGroup = ammoListContainer.GetComponent<HorizontalLayoutGroup>();

        if (clipSize > 20)
        {
            float reductionFactor = (clipSize - 20) * 0.5f;
            layoutGroup.spacing = Mathf.Max(3f, 5f - reductionFactor); 
        }
        else
        {
            layoutGroup.spacing = 5f;
        }

        for (int i = 0; i < clipSize; i++)
        {
            GameObject toggleBullet = Instantiate(toggleBulletPrefab, ammoListContainer);

            Toggle toggle = toggleBullet.GetComponent<Toggle>();
            currentToggleBullets.Add(toggle);

            toggle.isOn = i < currentClipSize;
        }
    }

    public void ResetAllBullets()
    {
        foreach (Toggle toggle in currentToggleBullets)
        {
            toggle.isOn = true;
        }
    }

    private void ClearToggleBullets()
    {
        foreach (Toggle toggle in currentToggleBullets)
        {
            if (toggle != null)
            {
                Destroy(toggle.gameObject);
            }
        }
        currentToggleBullets.Clear();
    }

    public void ConsumeBullet()
    {
        for (int i = currentToggleBullets.Count - 1; i >= 0; i--)
        {
            if (currentToggleBullets[i].isOn)
            {
                currentToggleBullets[i].isOn = false;
                break;
            }
        }
    }

    public void StartReloadIndicator(float reloadTime)
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
        }

        reloadIndicatorObject.SetActive(true);
        reloadSlider.value = 0f;
        reloadCoroutine = StartCoroutine(AnimateReloadIndicator(reloadTime));
    }

    public void CancelReloadIndicator()
    {
        if (reloadCoroutine != null)
        {
            StopCoroutine(reloadCoroutine);
            reloadCoroutine = null;
        }

        reloadIndicatorObject.SetActive(false);
        reloadSlider.value = 0f;
    }

    private IEnumerator AnimateReloadIndicator(float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            reloadSlider.value = elapsedTime / duration;
            yield return null;
        }

        reloadSlider.value = 1f;
        yield return new WaitForSeconds(0.1f); // Short delay before hiding
        reloadIndicatorObject.SetActive(false);

        reloadCoroutine = null;
    }

    public void TriggerTextLerp(TextMeshProUGUI textElement, Color targetColor, float duration)
    {
        if (lerpCoroutines.ContainsKey(textElement) && lerpCoroutines[textElement] != null)
        {
            StopCoroutine(lerpCoroutines[textElement]);
        }

        lerpCoroutines[textElement] = StartCoroutine(LerpTextColor(textElement, targetColor, duration));
    }

    private IEnumerator LerpTextColor(TextMeshProUGUI textElement, Color targetColor, float duration)
    {
        Color originalColor = textElement.color;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            textElement.color = Color.Lerp(originalColor, targetColor, time / duration);
            yield return null;
        }

        textElement.color = targetColor;

        yield return new WaitForSeconds(0.1f);

        time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            textElement.color = Color.Lerp(targetColor, defaultAmmoTextColor, time / duration);
            yield return null;
        }

        textElement.color = defaultAmmoTextColor;

        lerpCoroutines[textElement] = null;
    }

    public void UpdateSynthiumText()
    {
       currencyText.text = PlayerInventory.Instance.Currency.ToString();
    }

    public void UpdateLevelDisplay()
    {
        LevelSystem levelSystem = PlayerInventory.Instance.LevelSystem;
        XPBarSlider.value = (float)levelSystem.Experience / levelSystem.ExperienceToNextLevel;
        LevelText.text = levelSystem.Level.ToString();
        XPText.text = $"{levelSystem.Experience} / {levelSystem.ExperienceToNextLevel}";
    }
}
