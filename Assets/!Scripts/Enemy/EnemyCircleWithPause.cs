using UnityEngine;

public class FinalCirclePatrol : MonoBehaviour
{
    [Header("Ustawienia Ruchu")]
    public float speed = 2.0f;
    public float rotationSpeed = 40.0f; 
    
    [Header("Ustawienia Czasu")]
    public float moveTime = 5.0f;
    public float pauseTime = 5.0f;

    private float timer;
    private bool isWalking = false; 
    private Vector3 moveDirection = Vector3.zero;
    
    private Animator anim;
    private EnemyGravity gravityScript; 
    private PullableObject pullable; // Dodane, aby stopować patrole przy telekinezie

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        anim = GetComponent<Animator>();
        gravityScript = GetComponent<EnemyGravity>(); 
        pullable = GetComponent<PullableObject>();
        timer = pauseTime; 
    }

    void Update()
    {
        // Jeśli obiekt jest trzymany telekinezą, nie patroluj
        if (pullable != null && pullable.isCaptured) return;

        moveDirection = Vector3.zero; 

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            isWalking = !isWalking;
            timer = isWalking ? moveTime : pauseTime;
        }

        if (isWalking)
        {
            CalculateCircleMovement();
        }

        // RUCH POZIOMY
        transform.position += moveDirection * Time.deltaTime;

        // GRAWITACJA (z zewnętrznego skryptu)
        if (gravityScript != null)
        {
            transform.position += Vector3.up * gravityScript.verticalVelocity * Time.deltaTime;
        }

        UpdateAnimator(); 
    }

    void CalculateCircleMovement()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        moveDirection = transform.forward * speed;
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        
        // Sprawdzamy uziemienie przez skrypt grawitacji (brak CC)
        bool grounded = (gravityScript != null && Mathf.Abs(gravityScript.verticalVelocity) < 0.5f);
        
        anim.SetBool(hashIsWalking, isWalking);
        anim.SetBool(hashOnGround, grounded);

        if (gravityScript != null)
        {
            anim.SetFloat(hashVertVel, gravityScript.verticalVelocity);
        }
    }

    // --- GIZMOSY --- (Bez zmian w logice rysowania)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        float turnInRadians = rotationSpeed * Mathf.Deg2Rad;
        if (Mathf.Abs(turnInRadians) < 0.001f) return;
        float calculatedRadius = speed / turnInRadians;
        Vector3 centerSide = transform.right * calculatedRadius;
        Vector3 predictedCenter = transform.position + centerSide;
        DrawCircle(predictedCenter, calculatedRadius);
    }

    void DrawCircle(Vector3 center, float r)
    {
        float segments = 30; 
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(r, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
}