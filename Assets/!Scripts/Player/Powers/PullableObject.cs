using UnityEngine;

public class PullableObject : MonoBehaviour
{
    public float pullSpeed = 25f;
    public float stopDistance = 0.1f;

    public bool isCaptured { get; private set; }

    private Transform anchor;
    private bool isPulled;
    private bool isPinned;
    private Collider enemyCollider;
    private Rigidbody rb;

    void Start()
    {
        enemyCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!isPulled || anchor == null) return;

        if (!isPinned)
        {
            transform.position = Vector3.MoveTowards(transform.position, anchor.position, pullSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, anchor.rotation, 8f * Time.deltaTime);

            if (Vector3.Distance(transform.position, anchor.position) < stopDistance)
            {
                PinToAnchor();
            }
        }
        else
        {
            transform.position = anchor.position;
            transform.rotation = anchor.rotation;
        }
    }

    private void PinToAnchor()
    {
        isPinned = true;
        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (enemyCollider != null) enemyCollider.isTrigger = true;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void StartPull(Transform targetAnchor)
    {
        isCaptured = true;
        if (transform.parent != null) transform.SetParent(null);
        
        anchor = targetAnchor;
        isPulled = true;
        isPinned = false;

        if (enemyCollider != null) enemyCollider.isTrigger = true;
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void StopPull()
    {
        isCaptured = false;
        isPulled = false;
        isPinned = false;
        anchor = null;

        transform.SetParent(null);

        if (enemyCollider != null) enemyCollider.isTrigger = false;
        if (rb != null)
        {
            rb.useGravity = true;
        }
    }
}