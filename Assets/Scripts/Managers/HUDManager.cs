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

    public float lerpSpeed = 2f;

   private Coroutine currentCoroutine;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Decrease health by a certain amount
    public void DecreaseHealth(float damageAmount, float currentHealth, float maxHealth)
    {
        // Calculate the new fill amount based on damage
        float newFillAmount = (currentHealth - damageAmount) / maxHealth;

        // Prevent going below 0 health
        newFillAmount = Mathf.Clamp(newFillAmount, 0, 1);

        wallHPBar.fillAmount = newFillAmount; // Update the main health bar instantly

        // Stop any previous coroutine to prevent overlapping animations
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        // Start the delay animation for health decrease
        currentCoroutine = StartCoroutine(AnimateDelayBarDown());
    }

    // Increase health by a certain amount
    public void IncreaseHealth(float healAmount, float currentHealth, float maxHealth)
    {
        // Calculate the new fill amount based on healing
        float newFillAmount = (currentHealth + healAmount) / maxHealth;

        // Prevent exceeding max health
        newFillAmount = Mathf.Clamp(newFillAmount, 0, 1);

        wallHPBarAnimate.fillAmount = newFillAmount; // Delay bar increases first

        // Stop any previous coroutine to prevent overlapping animations
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        // Start the animation for the main health bar to catch up
        currentCoroutine = StartCoroutine(AnimateBarUp());
    }

    // Coroutine to animate the delay bar when health decreases
    private IEnumerator AnimateDelayBarDown()
    {
        wallHPBarAnimate.color = Color.red; // Set delay bar to red for visual effect

        while (wallHPBarAnimate.fillAmount > wallHPBar.fillAmount)
        {
            // Lerp the delay bar downwards to match the main bar
            wallHPBarAnimate.fillAmount = Mathf.Lerp(wallHPBarAnimate.fillAmount, wallHPBar.fillAmount, Time.deltaTime * lerpSpeed);
            yield return null;
        }

        wallHPBarAnimate.fillAmount = wallHPBar.fillAmount; // Snap to final position
        wallHPBarAnimate.color = wallHPBar.color; // Reset color to normal
    }

    // Coroutine to animate the main health bar when health increases
    private IEnumerator AnimateBarUp()
    {
        wallHPBarAnimate.color = Color.green; // Set delay bar to green for visual effect

        while (wallHPBar.fillAmount < wallHPBarAnimate.fillAmount)
        {
            // Lerp the main bar upwards to match the delay bar
            wallHPBar.fillAmount = Mathf.Lerp(wallHPBar.fillAmount, wallHPBarAnimate.fillAmount, Time.deltaTime * lerpSpeed);
            yield return null;
        }

        wallHPBar.fillAmount = wallHPBarAnimate.fillAmount; // Snap to final position
        wallHPBarAnimate.color = wallHPBar.color; // Reset color to normal
    }

}
