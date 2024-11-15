using UnityEngine;

public class TestTarget : MonoBehaviour, IDamageable
{
    private Material material;
    private Color originalColor;

    void Start()
    {
        // Cache the material and original color
        material = GetComponent<Renderer>().material;
        originalColor = material.color;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Target hit for {damage} damage!");
        StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        material.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        material.color = originalColor;
    }
} 