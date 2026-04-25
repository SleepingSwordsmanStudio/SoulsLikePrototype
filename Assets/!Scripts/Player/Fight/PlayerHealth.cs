using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    // To jest kluczowa flaga dla systemu uniku
    public bool isInvulnerable = false;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable)
        {
            Debug.Log("Uniknięto obrażeń!");
            return;
        }

        currentHealth -= amount;
        Debug.Log("Gracz otrzymał obrażenia. HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Gracz zginął!");
        // Tutaj możesz dodać animację śmierci lub restart poziomu
    }
}