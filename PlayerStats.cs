using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("体力设置")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 5f;
    [SerializeField] private float staminaRegenDelay = 2f;

    [Header("性能优化设置")]
    [SerializeField] private float eventTriggerThreshold = 0.01f;
    [SerializeField] private float minEventInterval = 0.05f;

    private float currentStamina;
    private float lastStaminaUsageTime;
    private bool isRegenerating = false;

    private float lastEventValue;
    private float lastEventTime;
    private bool isInitialized;

    public event Action<float, float> OnStaminaChanged;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float StaminaPercentage => currentStamina / maxStamina;

    private const float EPSILON = 0.0001f;

    private void Awake()
    {
        currentStamina = maxStamina;
        lastStaminaUsageTime = -staminaRegenDelay;
        lastEventValue = currentStamina;
        lastEventTime = -minEventInterval;
        isInitialized = true;
    }

    private void Update()
    {
        HandleStaminaRegeneration();
    }

    private void HandleStaminaRegeneration()
    {
        if (Time.time - lastStaminaUsageTime >= staminaRegenDelay && currentStamina < maxStamina)
        {
            isRegenerating = true;
            RegenerateStamina();
        }
        else
        {
            isRegenerating = false;
        }
    }

    private void RegenerateStamina()
    {
        float previousValue = currentStamina;
        currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);

        if (Mathf.Abs(currentStamina - previousValue) > EPSILON)
        {
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        if (!isInitialized) return;
        if (OnStaminaChanged == null) return;

        float timeSinceLastEvent = Time.unscaledTime - lastEventTime;
        bool valueChangedEnough = Mathf.Abs(currentStamina - lastEventValue) >= eventTriggerThreshold;
        bool maxReached = Mathf.Abs(currentStamina - maxStamina) < EPSILON;
        bool minReached = Mathf.Abs(currentStamina - 0f) < EPSILON;

        bool shouldTrigger = valueChangedEnough || maxReached || minReached ||
                            (timeSinceLastEvent >= minEventInterval && Mathf.Abs(currentStamina - lastEventValue) > EPSILON);

        if (shouldTrigger)
        {
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
            lastEventValue = currentStamina;
            lastEventTime = Time.unscaledTime;
        }
    }

    public void ConsumeStamina(float amount)
    {
        if (amount <= 0) return;

        float previousValue = currentStamina;
        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        lastStaminaUsageTime = Time.time;
        isRegenerating = false;

        if (Mathf.Abs(currentStamina - previousValue) > EPSILON)
        {
            TriggerEventImmediate();
        }
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0) return;

        float previousValue = currentStamina;
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);

        if (Mathf.Abs(currentStamina - previousValue) > EPSILON)
        {
            TriggerEventImmediate();
        }
    }

    public void SetStamina(float value)
    {
        float previousValue = currentStamina;
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);

        if (Mathf.Abs(currentStamina - previousValue) > EPSILON)
        {
            TriggerEventImmediate();
        }
    }

    private void TriggerEventImmediate()
    {
        if (!isInitialized) return;

        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        lastEventValue = currentStamina;
        lastEventTime = Time.unscaledTime;
    }

    public bool HasEnoughStamina(float requiredAmount)
    {
        return currentStamina >= requiredAmount;
    }

    public void ResetStamina()
    {
        float previousValue = currentStamina;
        currentStamina = maxStamina;

        if (Mathf.Abs(currentStamina - previousValue) > EPSILON)
        {
            TriggerEventImmediate();
        }
    }

    public void ForceTriggerEvent()
    {
        TriggerEventImmediate();
    }

    private void OnValidate()
    {
        eventTriggerThreshold = Mathf.Max(0.001f, eventTriggerThreshold);
        minEventInterval = Mathf.Max(0.01f, minEventInterval);
        staminaRegenRate = Mathf.Max(0f, staminaRegenRate);
        staminaRegenDelay = Mathf.Max(0f, staminaRegenDelay);
        maxStamina = Mathf.Max(1f, maxStamina);
    }
}
