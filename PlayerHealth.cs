using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("血量设置")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool canRegenerate = false;
    [SerializeField] private float healthRegenRate = 1f;
    [SerializeField] private float regenDelayAfterDamage = 5f;

    [Header("性能优化设置")]
    [SerializeField] private float eventTriggerThreshold = 0.01f;
    [SerializeField] private float minEventInterval = 0.05f;

    private float currentHealth;
    private float lastDamageTime;
    private bool isDead = false;

    private float lastEventValue;
    private float lastEventTime;
    private bool isInitialized;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;
    public event Action OnDamaged;
    public event Action OnHealed;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    public bool IsDead => isDead;

    private const float EPSILON = 0.0001f;

    private void Awake()
    {
        currentHealth = maxHealth;
        lastDamageTime = -regenDelayAfterDamage;
        lastEventValue = currentHealth;
        lastEventTime = -minEventInterval;
        isInitialized = true;
    }

    private void Update()
    {
        HandleHealthRegeneration();
    }

    private void HandleHealthRegeneration()
    {
        if (!canRegenerate || isDead) return;
        if (Time.time - lastDamageTime < regenDelayAfterDamage) return;
        if (currentHealth >= maxHealth) return;

        float previousValue = currentHealth;
        currentHealth = Mathf.Min(currentHealth + healthRegenRate * Time.deltaTime, maxHealth);

        if (Mathf.Abs(currentHealth - previousValue) > EPSILON)
        {
            TryTriggerEvent();
        }
    }

    private void TryTriggerEvent()
    {
        if (!isInitialized) return;
        if (OnHealthChanged == null) return;

        float timeSinceLastEvent = Time.unscaledTime - lastEventTime;
        bool valueChangedEnough = Mathf.Abs(currentHealth - lastEventValue) >= eventTriggerThreshold;
        bool maxReached = Mathf.Abs(currentHealth - maxHealth) < EPSILON;
        bool minReached = Mathf.Abs(currentHealth - 0f) < EPSILON;

        bool shouldTrigger = valueChangedEnough || maxReached || minReached ||
                            (timeSinceLastEvent >= minEventInterval && Mathf.Abs(currentHealth - lastEventValue) > EPSILON);

        if (shouldTrigger)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            lastEventValue = currentHealth;
            lastEventTime = Time.unscaledTime;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0) return;

        float previousValue = currentHealth;
        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        lastDamageTime = Time.time;

        if (Mathf.Abs(currentHealth - previousValue) > EPSILON)
        {
            OnDamaged?.Invoke();
            TriggerEventImmediate();

            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0) return;
        if (currentHealth >= maxHealth) return;

        float previousValue = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (Mathf.Abs(currentHealth - previousValue) > EPSILON)
        {
            OnHealed?.Invoke();
            TriggerEventImmediate();
        }
    }

    public void SetHealth(float value)
    {
        float previousValue = currentHealth;
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);

        if (Mathf.Abs(currentHealth - previousValue) > EPSILON)
        {
            if (currentHealth < previousValue)
            {
                OnDamaged?.Invoke();
            }
            else if (currentHealth > previousValue)
            {
                OnHealed?.Invoke();
            }

            TriggerEventImmediate();

            if (currentHealth <= 0 && !isDead)
            {
                Die();
            }
        }
    }

    private void TriggerEventImmediate()
    {
        if (!isInitialized) return;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        lastEventValue = currentHealth;
        lastEventTime = Time.unscaledTime;
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
    }

    public void Revive()
    {
        isDead = false;
        currentHealth = maxHealth;
        lastDamageTime = -regenDelayAfterDamage;
        TriggerEventImmediate();
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        lastDamageTime = -regenDelayAfterDamage;
        TriggerEventImmediate();
    }

    public bool HasEnoughHealth(float requiredAmount)
    {
        return currentHealth >= requiredAmount;
    }

    public void ForceTriggerEvent()
    {
        TriggerEventImmediate();
    }

    private void OnValidate()
    {
        eventTriggerThreshold = Mathf.Max(0.001f, eventTriggerThreshold);
        minEventInterval = Mathf.Max(0.01f, minEventInterval);
        healthRegenRate = Mathf.Max(0f, healthRegenRate);
        regenDelayAfterDamage = Mathf.Max(0f, regenDelayAfterDamage);
        maxHealth = Mathf.Max(1f, maxHealth);
    }
}
