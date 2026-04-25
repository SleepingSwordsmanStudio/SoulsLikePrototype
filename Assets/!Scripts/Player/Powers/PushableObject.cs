using UnityEngine;

public class PushableObject : MonoBehaviour
{
    [Header("Ustawienia Pchnięcia")]
    [Range(0, 1)] public float resistance = 0.2f; 
    public float drag = 5f; // Jak szybko obiekt hamuje

    private Vector3 impactVelocity;
    private PullableObject pullable;

    void Start()
    {
        pullable = GetComponent<PullableObject>();
    }

    void Update()
    {
        // Jeśli obiekt jest trzymany przez telekinezę, resetujemy siłę pchnięcia
        if (pullable != null && pullable.isCaptured)
        {
            impactVelocity = Vector3.zero;
            return;
        }

        if (impactVelocity.magnitude > 0.1f)
        {
            // Przesuwamy obiekt bezpośrednio przez transform
            transform.position += impactVelocity * Time.deltaTime;
            
            // Wyhamowanie siły (Lerp do zera)
            impactVelocity = Vector3.Lerp(impactVelocity, Vector3.zero, drag * Time.deltaTime);
        }
    }

    public void ApplyPush(Vector3 direction, float force)
    {
        float finalForce = force * (1f - resistance);
        
        // Ignorujemy oś Y, żeby pchnięcie było czysto poziome
        Vector3 pushDir = new Vector3(direction.x, 0, direction.z).normalized;
        impactVelocity += pushDir * finalForce;
    }
}