using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Ustawienia Pocisku")]
    public float speed = 15f;
    public Vector3 direction;

    void Update()
    {
        // Poruszanie rakietą w zadanym kierunku
        transform.position += direction * speed * Time.deltaTime;

        // Opcjonalnie: obracanie rakiety w stronę lotu
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    // Metoda wywoływana przez TelekinesisSystem przy odbiciu
    public void Reflect(Vector3 newDirection)
    {
        direction = newDirection.normalized;
        
        // Opcjonalnie: zwiększ prędkość przy odbiciu, żeby gracz czuł siłę
        speed *= 1.2f; 

        Debug.Log("Rakieta została odbita w nowym kierunku!");
    }
}