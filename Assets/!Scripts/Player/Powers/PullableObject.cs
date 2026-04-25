using UnityEngine;

public class PullableObject : MonoBehaviour
{
    public float pullSpeed = 25f;
    public float stopDistance = 0.1f;

    public bool isCaptured { get; private set; }

    private EnemyGravity gravityScript;
    private Transform anchor;
    private bool isPulled;
    private bool isPinned;
    private Collider enemyCollider;

    void Start()
    {
        gravityScript = GetComponent<EnemyGravity>();
        enemyCollider = GetComponent<Collider>();
    }

    void Update()
    {
        if (!isPulled || anchor == null) return;

        if (!isPinned)
        {
            // FAZA 1: PRZYCIĄGANIE (Gładki dolot do dłoni)
            transform.position = Vector3.MoveTowards(transform.position, anchor.position, pullSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, anchor.rotation, 8f * Time.deltaTime);

            if (Vector3.Distance(transform.position, anchor.position) < stopDistance)
            {
                PinToAnchor();
            }
        }
        else
        {
            // FAZA 2: TRZYMANIE (Sztywne przypięcie)
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

        // Zamieniamy na Trigger podczas trzymania, aby nie "wypychał" gracza fizycznie,
        // ale miecz nadal go wykryje (OnTriggerEnter).
        if (enemyCollider != null) enemyCollider.isTrigger = true;
    }

    public void StartPull(Transform targetAnchor)
    {
        isCaptured = true;
        if (transform.parent != null) transform.SetParent(null);
        
        anchor = targetAnchor;
        isPulled = true;
        isPinned = false;

        if (gravityScript != null) gravityScript.enabled = false;
        // W locie też ustawiamy trigger, żeby nie blokował gracza
        if (enemyCollider != null) enemyCollider.isTrigger = true;
    }

    public void StopPull()
    {
        isCaptured = false;
        isPulled = false;
        isPinned = false;
        anchor = null;

        transform.SetParent(null);

        // Powrót do normalnej fizyki
        if (enemyCollider != null) enemyCollider.isTrigger = false;
        if (gravityScript != null) gravityScript.enabled = true;
    }
}