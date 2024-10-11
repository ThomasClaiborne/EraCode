using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("--Wall--")]
    public TextMeshProUGUI WallHPText;
    public Image wallHPBar;
    public Image wallHPBarAnimate;
    public Image wallHPBarBackground;

    [Header("--Weapon--")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponAmmoText;
    public TextMeshProUGUI weaponReloading;

    [Header("--Currency--")]
    public TextMeshProUGUI currencyText;

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
    }
    void Start()
    {
        defaultAmmoTextColor = weaponAmmoText.color;
        UpdateCurrencyText();
    }

    void Update()
    {
        
    }

    public void DecreaseHealth(float damageAmount, float currentHealth, float maxHealth)
    {
        float newFillAmount = (currentHealth - damageAmount) / maxHealth;

        newFillAmount = Mathf.Clamp(newFillAmount, 0, 1);

        wallHPBar.fillAmount = newFillAmount; 

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateDelayBarDown());
    }

    public void IncreaseHealth(float healAmount, float currentHealth, float maxHealth)
    {
        float newFillAmount = (currentHealth + healAmount) / maxHealth;

        newFillAmount = Mathf.Clamp(newFillAmount, 0, 1);

        wallHPBarAnimate.fillAmount = newFillAmount;

        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateBarUp());
    }

    private IEnumerator AnimateDelayBarDown()
    {
        wallHPBarAnimate.color = Color.red;

        while (wallHPBarAnimate.fillAmount > wallHPBar.fillAmount)
        {
            wallHPBarAnimate.fillAmount = Mathf.Lerp(wallHPBarAnimate.fillAmount, wallHPBar.fillAmount, Time.deltaTime * lerpSpeed);
            yield return null;
        }

        wallHPBarAnimate.fillAmount = wallHPBar.fillAmount; 
        wallHPBarAnimate.color = wallHPBar.color; 
    }

    private IEnumerator AnimateBarUp()
    {
        wallHPBarAnimate.color = Color.white; 

        while (wallHPBar.fillAmount < wallHPBarAnimate.fillAmount)
        {
            wallHPBar.fillAmount = Mathf.Lerp(wallHPBar.fillAmount, wallHPBarAnimate.fillAmount, Time.deltaTime * lerpSpeed);
            yield return null;
        }

        wallHPBar.fillAmount = wallHPBarAnimate.fillAmount;
        wallHPBarAnimate.color = wallHPBar.color;
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

    public void UpdateCurrencyText()
    {
       currencyText.text = $"Currency: {PlayerInventory.Instance.Currency}";
    }
}
