using UnityEngine;
using System;

public class PlayerStats : MonoBehaviour
{
    [Header("体力设置")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaRegenRate = 5f;
    [SerializeField] private float staminaRegenDelay = 2f;

    private float currentStamina;
    private float lastStaminaUsageTime;
    private bool isRegenerating = false;

    public event Action<float, float> OnStaminaChanged;

    public float MaxStamina => maxStamina;
    public float CurrentStamina => currentStamina;
    public float StaminaPercentage => currentStamina / maxStamina;

    private void Awake()
    {
        currentStamina = maxStamina;
        lastStaminaUsageTime = -staminaRegenDelay;
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
        currentStamina = Mathf.Min(currentStamina + staminaRegenRate * Time.deltaTime, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void ConsumeStamina(float amount)
    {
        if (amount <= 0) return;

        currentStamina = Mathf.Max(currentStamina - amount, 0f);
        lastStaminaUsageTime = Time.time;
        isRegenerating = false;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        if (amount <= 0) return;

        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0f, maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    public bool HasEnoughStamina(float requiredAmount)
    {
        return currentStamina >= requiredAmount;
    }

    public void ResetStamina()
    {
        currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }
}
