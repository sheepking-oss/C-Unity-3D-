using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Slider delayedStaminaSlider;
    [SerializeField] private Text staminaText;

    [Header("动画设置")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float delayedSmoothSpeed = 2f;
    [SerializeField] private bool showText = true;

    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color lowStaminaColor = Color.red;
    [SerializeField] private float lowStaminaThreshold = 0.3f;

    private PlayerStats playerStats;
    private Image sliderFill;
    private float targetValue;
    private float delayedTargetValue;

    private void Start()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("StaminaUI: 未找到 PlayerStats 组件");
            return;
        }

        if (staminaSlider != null)
        {
            sliderFill = staminaSlider.fillRect.GetComponent<Image>();
            staminaSlider.maxValue = playerStats.MaxStamina;
            staminaSlider.value = playerStats.CurrentStamina;
            targetValue = playerStats.CurrentStamina;
        }

        if (delayedStaminaSlider != null)
        {
            delayedStaminaSlider.maxValue = playerStats.MaxStamina;
            delayedStaminaSlider.value = playerStats.CurrentStamina;
            delayedTargetValue = playerStats.CurrentStamina;
        }

        playerStats.OnStaminaChanged += UpdateStaminaUI;
        UpdateStaminaUI(playerStats.CurrentStamina, playerStats.MaxStamina);
    }

    private void Update()
    {
        SmoothUpdateSliders();
    }

    private void SmoothUpdateSliders()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = Mathf.Lerp(staminaSlider.value, targetValue, smoothSpeed * Time.deltaTime);
        }

        if (delayedStaminaSlider != null)
        {
            delayedStaminaSlider.value = Mathf.Lerp(delayedStaminaSlider.value, delayedTargetValue, delayedSmoothSpeed * Time.deltaTime);
        }
    }

    private void UpdateStaminaUI(float currentStamina, float maxStamina)
    {
        targetValue = currentStamina;
        delayedTargetValue = currentStamina;

        UpdateSliderColor(currentStamina, maxStamina);
        UpdateStaminaText(currentStamina, maxStamina);
    }

    private void UpdateSliderColor(float currentStamina, float maxStamina)
    {
        if (sliderFill == null) return;

        float staminaPercentage = currentStamina / maxStamina;
        sliderFill.color = staminaPercentage <= lowStaminaThreshold ? lowStaminaColor : normalColor;
    }

    private void UpdateStaminaText(float currentStamina, float maxStamina)
    {
        if (staminaText == null || !showText) return;

        int current = Mathf.RoundToInt(currentStamina);
        int max = Mathf.RoundToInt(maxStamina);
        staminaText.text = $"{current} / {max}";
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStaminaChanged -= UpdateStaminaUI;
        }
    }
}
