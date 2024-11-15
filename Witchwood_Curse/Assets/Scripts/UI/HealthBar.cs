using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fill;
    [SerializeField] private Gradient healthGradient;
    
    public void SetMaxHealth(int maxHealth)
    {
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
        UpdateFillColor(1f);
    }
    
    public void UpdateHealth(int currentHealth)
    {
        healthSlider.value = currentHealth;
        float healthPercent = (float)currentHealth / healthSlider.maxValue;
        UpdateFillColor(healthPercent);
    }
    
    private void UpdateFillColor(float healthPercent)
    {
        fill.color = healthGradient.Evaluate(healthPercent);
    }
} 