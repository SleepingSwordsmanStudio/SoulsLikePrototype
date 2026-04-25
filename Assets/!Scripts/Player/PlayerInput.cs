using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Movement Keys")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dodgeKey = KeyCode.Z;

    [Header("Lock-On Keys")]
    public KeyCode lockOnKey = KeyCode.Q;
    public KeyCode switchTargetKey = KeyCode.LeftAlt;

    [Header("Telekinesis Keys")]
    public KeyCode pushKey = KeyCode.Alpha2;
    public KeyCode pullKey = KeyCode.Alpha3;

    // --- Properties dla Movementu ---
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool IsSprintPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsDodgePressed { get; private set; }
    public bool LockOnDown { get; private set; }
    public bool SwitchTargetDown { get; private set; }

    // --- Properties dla Telekinezy (Naprawa błędów CS1061) ---
    public bool PushDown => Input.GetKeyDown(pushKey);
    public bool PushHold => Input.GetKey(pushKey); // Dodane: potrzebne do AoE
    public bool PushUp   => Input.GetKeyUp(pushKey);   // Dodane: potrzebne do pchnięcia po puszczeniu

    public bool PullDown => Input.GetKeyDown(pullKey);
    public bool PullHold => Input.GetKey(pullKey); // Dodane: potrzebne do ciągłego przyciągania
    public bool PullUp   => Input.GetKeyUp(pullKey);   // Dodane: potrzebne do przerwania przyciągania

    public void ReadInput()
    {
        Horizontal = Input.GetAxisRaw(horizontalAxis);
        Vertical = Input.GetAxisRaw(verticalAxis);
        IsSprintPressed = Input.GetKey(sprintKey);
        IsJumpPressed = Input.GetKeyDown(jumpKey);
        IsDodgePressed = Input.GetKeyDown(dodgeKey);
        LockOnDown = Input.GetKeyDown(lockOnKey);
        SwitchTargetDown = Input.GetKeyDown(switchTargetKey);
    }

    public Vector3 GetMovementDirection(Transform relativeTo)
{
    if (relativeTo == null) return new Vector3(Horizontal, 0, Vertical).normalized;

    // Zamiast brać relativeTo.forward (które może pływać przez Slerp kamery), 
    // tworzymy czysty kierunek na podstawie kątów świata.
    
    // Pobieramy rotację kamery tylko w osi Y (horyzontalną)
    float cameraYaw = relativeTo.eulerAngles.y;
    Quaternion cameraRotationY = Quaternion.Euler(0, cameraYaw, 0);

    // Obliczamy kierunki ŚWIATA względem tej rotacji
    Vector3 forward = cameraRotationY * Vector3.forward;
    Vector3 right = cameraRotationY * Vector3.right;

    // To gwarantuje, że "przód" to zawsze przód kamery rzutowany na ziemię,
    // niezależnie od tego, czy kamera patrzy w dół, czy w górę.
    return (forward * Vertical + right * Horizontal).normalized;
}
}