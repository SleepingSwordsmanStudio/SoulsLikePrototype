using UnityEngine;
using System.Collections.Generic;

public class EnemyPatrolSystem : MonoBehaviour
{
    [Header("Ustawienia Trasy")]
    public List<Transform> waypoints;
    public float waitTimeAtPoint = 3.0f;
    public float arrivalDistance = 0.5f;

    [Header("Parametry Ruchu")]
    public float walkSpeed = 2.0f;
    public float rotationSpeed = 10.0f;

    [Header("Detekcja Ziemi")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.3f;

    private Animator anim;
    private Rigidbody rb;
    private PullableObject pullable;
    
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        pullable = GetComponent<PullableObject>();

        if (groundLayer == 0) groundLayer = LayerMask.GetMask("Default");

        if (waypoints.Count == 0)
        {
            Debug.LogWarning("Brak punktów trasy przypisanych do " + gameObject.name);
        }
    }

    void FixedUpdate()
    {
        if (pullable != null && pullable.isCaptured)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            UpdateAnimator(false, -5.0f, false);
            return;
        }

        rb.useGravity = true;
        bool grounded = CheckIfGrounded();

        if (waypoints.Count == 0)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.fixedDeltaTime;
            if (waitTimer <= 0) isWaiting = false;
            
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
        }
        else
        {
            HandlePatrol(grounded);
        }
    }

    void HandlePatrol(bool grounded)
    {
        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        Vector3 diff = targetPos - transform.position;
        diff.y = 0;

        if (diff.magnitude < arrivalDistance)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            UpdateAnimator(grounded, rb.linearVelocity.y, false);
        }
        else
        {
            Quaternion targetRot = Quaternion.LookRotation(diff);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime));

            Vector3 vel = transform.forward * walkSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
            UpdateAnimator(grounded, rb.linearVelocity.y, true);
        }
    }

    bool CheckIfGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance, groundLayer);
    }

    void UpdateAnimator(bool grounded, float vVel, bool moving)
    {
        if (anim == null) return;
        anim.SetBool(hashIsWalking, moving);
        anim.SetBool(hashOnGround, grounded);
        anim.SetFloat(hashVertVel, vVel);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            int nextIndex = (i + 1) % waypoints.Count;
            if (waypoints[nextIndex] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundCheckDistance);
    }
}