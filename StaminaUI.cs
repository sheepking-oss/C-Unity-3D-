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

    [Header("性能优化设置")]
    [SerializeField] private float valueChangeThreshold = 0.01f;
    [SerializeField] private float minUpdateInterval = 0.02f;
    [SerializeField] private bool useLateUpdate = true;

    private PlayerStats playerStats;
    private Image sliderFill;
    private Canvas parentCanvas;

    private float targetValue;
    private float delayedTargetValue;
    private float maxStamina;

    private float currentSliderValue;
    private float currentDelayedSliderValue;
    private float lastUpdateTime;

    private int cachedCurrentInt;
    private int cachedMaxInt;
    private Color cachedColor;
    private bool isLowStamina;

    private bool needsUpdate;
    private bool isInitialized;

    private const float EPSILON = 0.0001f;

    private void Awake()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (staminaSlider != null)
        {
            sliderFill = staminaSlider.fillRect.GetComponent<Image>();
            currentSliderValue = staminaSlider.value;
        }

        if (delayedStaminaSlider != null)
        {
            currentDelayedSliderValue = delayedStaminaSlider.value;
        }
    }

    private void Start()
    {
        FindAndBindPlayerStats();
    }

    private void FindAndBindPlayerStats()
    {
        playerStats = FindObjectOfType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogWarning("StaminaUI: 未找到 PlayerStats 组件");
            return;
        }

        maxStamina = playerStats.MaxStamina;
        float currentStamina = playerStats.CurrentStamina;

        InitializeUIValues(currentStamina, maxStamina);

        playerStats.OnStaminaChanged += OnStaminaValueChanged;
        isInitialized = true;
    }

    private void InitializeUIValues(float current, float max)
    {
        targetValue = current;
        delayedTargetValue = current;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = max;
            staminaSlider.value = current;
            currentSliderValue = current;
        }

        if (delayedStaminaSlider != null)
        {
            delayedStaminaSlider.maxValue = max;
            delayedStaminaSlider.value = current;
            currentDelayedSliderValue = current;
        }

        cachedCurrentInt = Mathf.RoundToInt(current);
        cachedMaxInt = Mathf.RoundToInt(max);

        float percentage = current / max;
        isLowStamina = percentage <= lowStaminaThreshold;
        cachedColor = isLowStamina ? lowStaminaColor : normalColor;

        UpdateSliderColorImmediate();
        UpdateStaminaTextImmediate();
    }

    private void OnStaminaValueChanged(float current, float max)
    {
        if (Mathf.Abs(targetValue - current) < valueChangeThreshold && Mathf.Abs(maxStamina - max) < EPSILON)
        {
            return;
        }

        targetValue = current;
        delayedTargetValue = current;

        if (Mathf.Abs(maxStamina - max) > EPSILON)
        {
            maxStamina = max;
            cachedMaxInt = Mathf.RoundToInt(max);

            if (staminaSlider != null)
            {
                staminaSlider.maxValue = max;
            }
            if (delayedStaminaSlider != null)
            {
                delayedStaminaSlider.maxValue = max;
            }
        }

        needsUpdate = true;
        CheckColorAndTextUpdate(current, max);
    }

    private void CheckColorAndTextUpdate(float current, float max)
    {
        float percentage = current / max;
        bool newIsLowStamina = percentage <= lowStaminaThreshold;

        if (newIsLowStamina != isLowStamina)
        {
            isLowStamina = newIsLowStamina;
            cachedColor = isLowStamina ? lowStaminaColor : normalColor;
            UpdateSliderColorImmediate();
        }

        int newCurrentInt = Mathf.RoundToInt(current);
        if (newCurrentInt != cachedCurrentInt)
        {
            cachedCurrentInt = newCurrentInt;
            UpdateStaminaTextImmediate();
        }
    }

    private void Update()
    {
        if (!useLateUpdate && needsUpdate)
        {
            PerformSmoothUpdate();
        }
    }

    private void LateUpdate()
    {
        if (useLateUpdate && needsUpdate)
        {
            PerformSmoothUpdate();
        }
    }

    private void PerformSmoothUpdate()
    {
        if (!isInitialized) return;

        float timeSinceLastUpdate = Time.unscaledTime - lastUpdateTime;
        if (timeSinceLastUpdate < minUpdateInterval)
        {
            return;
        }

        bool sliderUpdated = UpdateMainSlider();
        bool delayedSliderUpdated = UpdateDelayedSlider();

        if (!sliderUpdated && !delayedSliderUpdated)
        {
            needsUpdate = false;
        }

        lastUpdateTime = Time.unscaledTime;
    }

    private bool UpdateMainSlider()
    {
        if (staminaSlider == null) return false;

        if (Mathf.Abs(currentSliderValue - targetValue) < valueChangeThreshold)
        {
            if (Mathf.Abs(staminaSlider.value - targetValue) > EPSILON)
            {
                staminaSlider.value = targetValue;
                currentSliderValue = targetValue;
            }
            return false;
        }

        float newValue = Mathf.Lerp(currentSliderValue, targetValue, smoothSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(newValue - currentSliderValue) > valueChangeThreshold)
        {
            staminaSlider.value = newValue;
            currentSliderValue = newValue;
            return true;
        }

        return false;
    }

    private bool UpdateDelayedSlider()
    {
        if (delayedStaminaSlider == null) return false;

        if (Mathf.Abs(currentDelayedSliderValue - delayedTargetValue) < valueChangeThreshold)
        {
            if (Mathf.Abs(delayedStaminaSlider.value - delayedTargetValue) > EPSILON)
            {
                delayedStaminaSlider.value = delayedTargetValue;
                currentDelayedSliderValue = delayedTargetValue;
            }
            return false;
        }

        float newValue = Mathf.Lerp(currentDelayedSliderValue, delayedTargetValue, delayedSmoothSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(newValue - currentDelayedSliderValue) > valueChangeThreshold)
        {
            delayedStaminaSlider.value = newValue;
            currentDelayedSliderValue = newValue;
            return true;
        }

        return false;
    }

    private void UpdateSliderColorImmediate()
    {
        if (sliderFill != null)
        {
            sliderFill.color = cachedColor;
        }
    }

    private void UpdateStaminaTextImmediate()
    {
        if (staminaText == null || !showText) return;
        staminaText.text = $"{cachedCurrentInt} / {cachedMaxInt}";
    }

    public void ForceRefresh()
    {
        if (playerStats != null)
        {
            targetValue = playerStats.CurrentStamina;
            delayedTargetValue = playerStats.CurrentStamina;
            needsUpdate = true;

            CheckColorAndTextUpdate(playerStats.CurrentStamina, playerStats.MaxStamina);

            if (staminaSlider != null)
            {
                staminaSlider.value = targetValue;
                currentSliderValue = targetValue;
            }
            if (delayedStaminaSlider != null)
            {
                delayedStaminaSlider.value = delayedTargetValue;
                currentDelayedSliderValue = delayedTargetValue;
            }
        }
    }

    public void SetTargetValueDirectly(float value)
    {
        targetValue = Mathf.Clamp(value, 0f, maxStamina);
        delayedTargetValue = targetValue;
        needsUpdate = true;

        if (playerStats != null)
        {
            CheckColorAndTextUpdate(targetValue, maxStamina);
        }
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnStaminaChanged -= OnStaminaValueChanged;
        }
    }

    private void OnValidate()
    {
        valueChangeThreshold = Mathf.Max(0.001f, valueChangeThreshold);
        minUpdateInterval = Mathf.Max(0.001f, minUpdateInterval);
        smoothSpeed = Mathf.Max(0.1f, smoothSpeed);
        delayedSmoothSpeed = Mathf.Max(0.1f, delayedSmoothSpeed);
    }
}
