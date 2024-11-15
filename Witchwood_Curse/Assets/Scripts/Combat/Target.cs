using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour, IDamageable, IBurnable
{
    private Material material;
    private Color originalColor;

    void Start()
    {
        // Get the renderer's material
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Target took {damage} damage!");
        StartCoroutine(FlashRed());
    }
    
    public void ApplyBurn(float damage, float duration)
    {
        Debug.Log($"Target burning for {damage} damage over {duration} seconds!");
    }

    private IEnumerator FlashRed()
    {
        material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        material.color = originalColor;
    }
} 