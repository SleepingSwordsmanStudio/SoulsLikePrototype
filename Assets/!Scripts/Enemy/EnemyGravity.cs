using UnityEngine;

public class EnemyGravity : MonoBehaviour
{
    public float gravity = -20f;
    public float verticalVelocity;
    
    [Header("Detekcja Podłoża")]
    public float groundCheckDistance = 0.5f; // Dłuższy promień = bezpieczniej
    public float raycastOffset = 0.5f;       // Startujemy z poziomu kolan
    public LayerMask groundLayer;            // UPEWNIJ SIĘ, ŻE TO NIE JEST "NOTHING"

    private PullableObject pullable;

    void Start()
    {
        pullable = GetComponent<PullableObject>();
        
        // Zabezpieczenie: Jeśli zapomniałeś ustawić warstwy, ustawiamy na Default
        if (groundLayer == 0) 
        {
            groundLayer = LayerMask.GetMask("Default");
            Debug.LogWarning("Ground Layer nie ustawiony w " + name + "! Ustawiam domyślnie na Default.");
        }
    }

    void Update()
    {
        // Jeśli wróg lewituje (telekineza), stopujemy grawitację
        if (pullable != null && pullable.isCaptured)
        {
            verticalVelocity = 0f;
            return;
        }

        if (CheckIfGrounded())
        {
            // Jeśli dotykamy ziemi i spadamy, zerujemy prędkość
            if (verticalVelocity < 0)
            {
                verticalVelocity = -0.5f; // Lekki docisk, żeby nie "drżał" na schodach
            }
        }
        else
        {
            // Swobodne spadanie
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Aplikacja ruchu
        transform.position += Vector3.up * verticalVelocity * Time.deltaTime;
    }

    public bool CheckIfGrounded()
    {
        // Strzelamy z poziomu kolan (raycastOffset) w dół
        // Promień musi być dłuższy niż offset, żeby wystawał pod stopy
        Vector3 origin = transform.position + Vector3.up * raycastOffset;
        bool hit = Physics.Raycast(origin, Vector3.down, raycastOffset + groundCheckDistance, groundLayer);

        return hit;
    }

    // Rysowanie w Scene View (niebieska linia to zasięg nogi szkieleta)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.up * raycastOffset;
        Gizmos.DrawLine(origin, origin + Vector3.down * (raycastOffset + groundCheckDistance));
    }
}