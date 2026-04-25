using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Transform player; 
    public LockOnSystem lockSystem;
    public PlayerClimbing climbingScript; // PRZYPISZ W INSPEKTORZE

    [Header("General Settings")]
    public Vector3 offset = new Vector3(0, 2f, -4f);
    public float smoothSpeed = 15f;
    public LayerMask collisionLayers;
    public float cameraRadius = 0.2f;

    [Header("Climbing Settings")]
    public float climbDistance = 7f; // Dystans kamery podczas wspinaczki
    public float zoomSpeed = 5f;     // Prędkość oddalania/przybliżania

    [Header("Input")]
    public float mouseSensitivity = 100f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    private float mouseX, mouseY;
    private float currentDistance;
    private float defaultDistance;
    private float targetBaseDistance; // Aktualny cel dystansu (podstawowy lub wspinaczkowy)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        if (lockSystem == null && player != null) 
            lockSystem = player.GetComponent<LockOnSystem>();
        
        // Szukamy skryptu wspinaczki jeśli nie przypisano
        if (climbingScript == null && player != null)
            climbingScript = player.GetComponent<PlayerClimbing>();
            
        defaultDistance = new Vector3(offset.x, 0, offset.z).magnitude;
        if (defaultDistance < 1f) defaultDistance = 4f;
        
        targetBaseDistance = defaultDistance;
        currentDistance = defaultDistance;
        
        Vector3 rot = transform.eulerAngles;
        mouseX = rot.y;
        mouseY = rot.x;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // DYNAMIKA DYSTANSU
        HandleDynamicDistance();

        if (lockSystem != null && lockSystem.IsLockedOn && lockSystem.currentTarget != null)
        {
            HandleLockedCamera();
        }
        else
        {
            HandleFreeCamera();
        }
    }

    void HandleDynamicDistance()
    {
        // Jeśli gracz się wspina, celem jest climbDistance, w przeciwnym razie defaultDistance
        float goal = (climbingScript != null && climbingScript.isClimbing) ? climbDistance : defaultDistance;
        
        // Płynne przejście między dystansami
        targetBaseDistance = Mathf.Lerp(targetBaseDistance, goal, Time.deltaTime * zoomSpeed);
    }

    void HandleFreeCamera()
    {
        mouseX += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseY -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        mouseY = Mathf.Clamp(mouseY, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(mouseY, mouseX, 0);
        ApplyCameraPosition(rotation);
    }

    void HandleLockedCamera()
    {
        Vector3 targetPos = lockSystem.currentTarget.position + Vector3.up * 1f;
        Vector3 playerPos = player.position + Vector3.up * offset.y;
        
        Vector3 dirToTarget = (targetPos - playerPos).normalized;
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
        
        Vector3 euler = targetRot.eulerAngles;
        float angleX = euler.x > 180 ? euler.x - 360 : euler.x;
        angleX = Mathf.Clamp(angleX, -15f, 25f); 
        
        Quaternion finalRot = Quaternion.Euler(angleX, euler.y, 0);
        ApplyCameraPosition(finalRot);

        mouseX = transform.eulerAngles.y;
        mouseY = transform.eulerAngles.x;
    }

    void ApplyCameraPosition(Quaternion rotation)
    {
        Vector3 rayStart = player.position + Vector3.up * offset.y;
        Vector3 rayDir = rotation * Vector3.back;

        // Używamy targetBaseDistance zamiast defaultDistance, aby kolizja uwzględniała zoom
        if (Physics.SphereCast(rayStart, cameraRadius, rayDir, out RaycastHit hit, targetBaseDistance, collisionLayers))
        {
            currentDistance = Mathf.Lerp(currentDistance, hit.distance - 0.1f, Time.deltaTime * 10f);
        }
        else
        {
            currentDistance = Mathf.Lerp(currentDistance, targetBaseDistance, Time.deltaTime * 10f);
        }

        Vector3 desiredPos = rayStart + (rayDir * currentDistance);
        
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, smoothSpeed * Time.deltaTime);
    }
}