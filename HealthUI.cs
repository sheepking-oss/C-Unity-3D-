using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("UI组件引用")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider delayedHealthSlider;
    [SerializeField] private Text healthText;

    [Header("动画设置")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float delayedSmoothSpeed = 3f;
    [SerializeField] private bool showText = true;

    [Header("颜色设置")]
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color criticalHealthColor = new Color(1f, 0f, 0f);
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private float criticalHealthThreshold = 0.15f;

    [Header("性能优化设置")]
    [SerializeField] private float valueChangeThreshold = 0.01f;
    [SerializeField] private float minUpdateInterval = 0.02f;
    [SerializeField] private bool useLateUpdate = true;

    [Header("特殊效果")]
    [SerializeField] private bool enableDamageFlash = true;
    [SerializeField] private bool enableHealFlash = true;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private Color healFlashColor = Color.green;

    private PlayerHealth playerHealth;
    private Image sliderFill;
    private Canvas parentCanvas;

    private float targetValue;
    private float delayedTargetValue;
    private float maxHealth;

    private float currentSliderValue;
    private float currentDelayedSliderValue;
    private float lastUpdateTime;

    private int cachedCurrentInt;
    private int cachedMaxInt;
    private Color cachedColor;
    private int healthState;

    private bool needsUpdate;
    private bool isInitialized;

    private bool isFlashing;
    private float flashStartTime;
    private Color flashTargetColor;
    private Color originalFillColor;

    private const float EPSILON = 0.0001f;
    private const int HEALTH_NORMAL = 0;
    private const int HEALTH_LOW = 1;
    private const int HEALTH_CRITICAL = 2;

    private void Awake()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (healthSlider != null)
        {
            sliderFill = healthSlider.fillRect.GetComponent<Image>();
            currentSliderValue = healthSlider.value;
            if (sliderFill != null)
            {
                originalFillColor = sliderFill.color;
            }
        }

        if (delayedHealthSlider != null)
        {
            currentDelayedSliderValue = delayedHealthSlider.value;
        }
    }

    private void Start()
    {
        FindAndBindPlayerHealth();
    }

    private void FindAndBindPlayerHealth()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("HealthUI: 未找到 PlayerHealth 组件");
            return;
        }

        maxHealth = playerHealth.MaxHealth;
        float currentHealth = playerHealth.CurrentHealth;

        InitializeUIValues(currentHealth, maxHealth);

        playerHealth.OnHealthChanged += OnHealthValueChanged;
        playerHealth.OnDamaged += OnDamaged;
        playerHealth.OnHealed += OnHealed;
        playerHealth.OnDeath += OnDeath;

        isInitialized = true;
    }

    private void InitializeUIValues(float current, float max)
    {
        targetValue = current;
        delayedTargetValue = current;

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
            currentSliderValue = current;
        }

        if (delayedHealthSlider != null)
        {
            delayedHealthSlider.maxValue = max;
            delayedHealthSlider.value = current;
            currentDelayedSliderValue = current;
        }

        cachedCurrentInt = Mathf.RoundToInt(current);
        cachedMaxInt = Mathf.RoundToInt(max);

        UpdateHealthState(current, max);
        cachedColor = GetColorForHealthState(healthState);

        UpdateSliderColorImmediate();
        UpdateHealthTextImmediate();
    }

    private void OnHealthValueChanged(float current, float max)
    {
        if (Mathf.Abs(targetValue - current) < valueChangeThreshold && Mathf.Abs(maxHealth - max) < EPSILON)
        {
            return;
        }

        targetValue = current;
        delayedTargetValue = current;

        if (Mathf.Abs(maxHealth - max) > EPSILON)
        {
            maxHealth = max;
            cachedMaxInt = Mathf.RoundToInt(max);

            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
            }
            if (delayedHealthSlider != null)
            {
                delayedHealthSlider.maxValue = max;
            }
        }

        needsUpdate = true;
        CheckColorAndTextUpdate(current, max);
    }

    private void CheckColorAndTextUpdate(float current, float max)
    {
        UpdateHealthState(current, max);
        int newCurrentInt = Mathf.RoundToInt(current);

        if (newCurrentInt != cachedCurrentInt)
        {
            cachedCurrentInt = newCurrentInt;
            UpdateHealthTextImmediate();
        }
    }

    private void UpdateHealthState(float current, float max)
    {
        float percentage = current / max;
        int newHealthState;

        if (percentage <= criticalHealthThreshold)
        {
            newHealthState = HEALTH_CRITICAL;
        }
        else if (percentage <= lowHealthThreshold)
        {
            newHealthState = HEALTH_LOW;
        }
        else
        {
            newHealthState = HEALTH_NORMAL;
        }

        if (newHealthState != healthState)
        {
            healthState = newHealthState;
            cachedColor = GetColorForHealthState(healthState);
            UpdateSliderColorImmediate();
        }
    }

    private Color GetColorForHealthState(int state)
    {
        switch (state)
        {
            case HEALTH_CRITICAL:
                return criticalHealthColor;
            case HEALTH_LOW:
                return lowHealthColor;
            default:
                return normalColor;
        }
    }

    private void OnDamaged()
    {
        if (enableDamageFlash)
        {
            StartFlash(damageFlashColor);
        }
    }

    private void OnHealed()
    {
        if (enableHealFlash)
        {
            StartFlash(healFlashColor);
        }
    }

    private void OnDeath()
    {
        needsUpdate = true;
        targetValue = 0f;
        delayedTargetValue = 0f;
    }

    private void StartFlash(Color flashColor)
    {
        if (sliderFill == null) return;

        isFlashing = true;
        flashStartTime = Time.unscaledTime;
        flashTargetColor = flashColor;
        sliderFill.color = flashColor;
    }

    private void Update()
    {
        HandleFlashEffect();

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

    private void HandleFlashEffect()
    {
        if (!isFlashing || sliderFill == null) return;

        float elapsed = Time.unscaledTime - flashStartTime;
        if (elapsed >= flashDuration)
        {
            isFlashing = false;
            sliderFill.color = cachedColor;
            return;
        }

        float t = elapsed / flashDuration;
        sliderFill.color = Color.Lerp(flashTargetColor, cachedColor, t);
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
        if (healthSlider == null) return false;

        if (Mathf.Abs(currentSliderValue - targetValue) < valueChangeThreshold)
        {
            if (Mathf.Abs(healthSlider.value - targetValue) > EPSILON)
            {
                healthSlider.value = targetValue;
                currentSliderValue = targetValue;
            }
            return false;
        }

        float newValue = Mathf.Lerp(currentSliderValue, targetValue, smoothSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(newValue - currentSliderValue) > valueChangeThreshold)
        {
            healthSlider.value = newValue;
            currentSliderValue = newValue;
            return true;
        }

        return false;
    }

    private bool UpdateDelayedSlider()
    {
        if (delayedHealthSlider == null) return false;

        if (Mathf.Abs(currentDelayedSliderValue - delayedTargetValue) < valueChangeThreshold)
        {
            if (Mathf.Abs(delayedHealthSlider.value - delayedTargetValue) > EPSILON)
            {
                delayedHealthSlider.value = delayedTargetValue;
                currentDelayedSliderValue = delayedTargetValue;
            }
            return false;
        }

        float newValue = Mathf.Lerp(currentDelayedSliderValue, delayedTargetValue, delayedSmoothSpeed * Time.unscaledDeltaTime);

        if (Mathf.Abs(newValue - currentDelayedSliderValue) > valueChangeThreshold)
        {
            delayedHealthSlider.value = newValue;
            currentDelayedSliderValue = newValue;
            return true;
        }

        return false;
    }

    private void UpdateSliderColorImmediate()
    {
        if (sliderFill != null && !isFlashing)
        {
            sliderFill.color = cachedColor;
        }
    }

    private void UpdateHealthTextImmediate()
    {
        if (healthText == null || !showText) return;
        healthText.text = $"{cachedCurrentInt} / {cachedMaxInt}";
    }

    public void ForceRefresh()
    {
        if (playerHealth != null)
        {
            targetValue = playerHealth.CurrentHealth;
            delayedTargetValue = playerHealth.CurrentHealth;
            needsUpdate = true;

            UpdateHealthState(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            CheckColorAndTextUpdate(playerHealth.CurrentHealth, playerHealth.MaxHealth);

            if (healthSlider != null)
            {
                healthSlider.value = targetValue;
                currentSliderValue = targetValue;
            }
            if (delayedHealthSlider != null)
            {
                delayedHealthSlider.value = delayedTargetValue;
                currentDelayedSliderValue = delayedTargetValue;
            }
        }
    }

    public void SetTargetValueDirectly(float value)
    {
        targetValue = Mathf.Clamp(value, 0f, maxHealth);
        delayedTargetValue = targetValue;
        needsUpdate = true;

        if (playerHealth != null)
        {
            UpdateHealthState(targetValue, maxHealth);
            CheckColorAndTextUpdate(targetValue, maxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= OnHealthValueChanged;
            playerHealth.OnDamaged -= OnDamaged;
            playerHealth.OnHealed -= OnHealed;
            playerHealth.OnDeath -= OnDeath;
        }
    }

    private void OnValidate()
    {
        valueChangeThreshold = Mathf.Max(0.001f, valueChangeThreshold);
        minUpdateInterval = Mathf.Max(0.001f, minUpdateInterval);
        smoothSpeed = Mathf.Max(0.1f, smoothSpeed);
        delayedSmoothSpeed = Mathf.Max(0.1f, delayedSmoothSpeed);
        lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
        criticalHealthThreshold = Mathf.Clamp01(criticalHealthThreshold);
        flashDuration = Mathf.Max(0.01f, flashDuration);

        if (criticalHealthThreshold > lowHealthThreshold)
        {
            criticalHealthThreshold = lowHealthThreshold * 0.5f;
        }
    }
}
