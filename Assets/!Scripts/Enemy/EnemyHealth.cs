using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        // Kolorowy debug w konsoli ułatwia czytanie
        Debug.Log($"<color=cyan>[HP Przeciwnika]</color> Otrzymano: {damage} | Pozostało HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("<color=black><b>Przeciwnik UMARŁ!</b></color>");
        // Destroy(gameObject); // Opcjonalnie usuń obiekt
    }
}