using UnityEngine;

public class FinalCirclePatrol : MonoBehaviour
{
    [Header("Ustawienia Ruchu")]
    public float speed = 2.0f;
    public float rotationSpeed = 40.0f; 
    
    [Header("Ustawienia Czasu")]
    public float moveTime = 5.0f;
    public float pauseTime = 5.0f;

    [Header("Detekcja Ziemi")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    private float timer;
    private bool isWalking = false; 
    
    private Animator anim;
    private Rigidbody rb;
    private PullableObject pullable;

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        pullable = GetComponent<PullableObject>();
        
        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default");
        timer = pauseTime; 
    }

    void FixedUpdate()
    {
        if (pullable != null && pullable.isCaptured)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            UpdateAnimator(false, -5.0f);
            return;
        }

        rb.useGravity = true;

        timer -= Time.fixedDeltaTime;
        if (timer <= 0)
        {
            isWalking = !isWalking;
            timer = isWalking ? moveTime : pauseTime;
        }

        if (isWalking)
        {
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, rotationSpeed * Time.fixedDeltaTime, 0));
            rb.MoveRotation(rb.rotation * deltaRotation);

            Vector3 targetVel = transform.forward * speed;
            targetVel.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVel;
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        UpdateAnimator(CheckIfGrounded(), rb.linearVelocity.y);
    }

    bool CheckIfGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }

    void UpdateAnimator(bool grounded, float vVel)
    {
        if (anim == null) return;
        
        anim.SetBool(hashIsWalking, isWalking);
        anim.SetBool(hashOnGround, grounded);
        anim.SetFloat(hashVertVel, vVel);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        float turnInRadians = rotationSpeed * Mathf.Deg2Rad;
        if (Mathf.Abs(turnInRadians) < 0.001f) return;
        float calculatedRadius = speed / turnInRadians;
        Vector3 predictedCenter = transform.position + transform.right * calculatedRadius;
        
        float segments = 30; 
        float angleStep = 360f / segments;
        Vector3 prevPoint = predictedCenter + new Vector3(calculatedRadius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float a = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = predictedCenter + new Vector3(Mathf.Cos(a) * calculatedRadius, 0, Mathf.Sin(a) * calculatedRadius);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance);
    }
}