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

    private Animator anim;
    private EnemyGravity gravityScript; 
    private PullableObject pullable;
    
    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private Vector3 moveDirection = Vector3.zero;

    private readonly int hashIsWalking = Animator.StringToHash("IsWalking");
    private readonly int hashOnGround = Animator.StringToHash("OnGround");
    private readonly int hashVertVel = Animator.StringToHash("VerticalVelocity");

    void Start()
    {
        anim = GetComponent<Animator>();
        gravityScript = GetComponent<EnemyGravity>();
        pullable = GetComponent<PullableObject>();

        if (waypoints.Count == 0)
        {
            Debug.LogWarning("Brak punktów trasy przypisanych do " + gameObject.name);
        }
    }

    void Update()
    {
        // Blokada ruchu podczas telekinezy
        if (pullable != null && pullable.isCaptured) return;

        moveDirection = Vector3.zero;

        HandlePatrol();

        // RUCH POZIOMY
        transform.position += moveDirection * Time.deltaTime;

        // GRAWITACJA
        if (gravityScript != null)
        {
            transform.position += Vector3.up * gravityScript.verticalVelocity * Time.deltaTime;
        }

        UpdateAnimator();
    }

    void HandlePatrol()
    {
        if (waypoints.Count == 0 || isWaiting) return;

        Vector2 enemyPos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 targetPos2D = new Vector2(waypoints[currentWaypointIndex].position.x, waypoints[currentWaypointIndex].position.z);

        if (Vector2.Distance(enemyPos2D, targetPos2D) < arrivalDistance)
        {
            StartCoroutine(WaitAtPoint());
        }
        else
        {
            CalculateMoveVector();
        }
    }

    void CalculateMoveVector()
    {
        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        Vector3 direction = (targetPos - transform.position);
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            moveDirection = transform.forward * walkSpeed;
        }
    }

    System.Collections.IEnumerator WaitAtPoint()
    {
        isWaiting = true;
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
        yield return new WaitForSeconds(waitTimeAtPoint);
        isWaiting = false;
    }

    void UpdateAnimator()
    {
        if (anim == null) return;
        
        bool moving = !isWaiting && waypoints.Count > 0 && (moveDirection.x != 0 || moveDirection.z != 0);
        bool grounded = (gravityScript != null && Mathf.Abs(gravityScript.verticalVelocity) < 0.5f);

        anim.SetBool(hashIsWalking, moving);
        anim.SetBool(hashOnGround, grounded);
        
        if (gravityScript != null) 
            anim.SetFloat(hashVertVel, gravityScript.verticalVelocity);
    }

    void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawSphere(waypoints[i].position, 0.3f);
            int nextIndex = (i + 1) % waypoints.Count;
            if (waypoints[nextIndex] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[nextIndex].position);
        }
    }
}