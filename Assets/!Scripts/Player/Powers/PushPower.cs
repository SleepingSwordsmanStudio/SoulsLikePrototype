using UnityEngine;

public class PushPower : MonoBehaviour
{
    public float pushForce = 20f;
    public float launchForce = 35f; 
    public float holdThreshold = 1.5f;
    
    private TelekinesisSystem system;
    private float holdTimer;
    private bool hasDischarged;

    public void Initialize(TelekinesisSystem sys) => system = sys;

    public void ProcessInput(bool down, bool held, bool up)
    {
        if (down) 
        { 
            holdTimer = 0; 
            hasDischarged = false; 

            if (system.IsCurrentlyPulling)
            {
                ExecuteLaunchHeldObject();
                hasDischarged = true; 
                return;
            }
        }

        if (held)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdThreshold && !hasDischarged)
            {
                ExecuteAoEPush();
                hasDischarged = true;
            }
        }

        if (up && !hasDischarged) ExecuteSinglePush();
    }

    void ExecuteLaunchHeldObject()
    {
        if (system.lockOn != null && system.lockOn.currentTarget != null)
        {
            Transform target = system.lockOn.currentTarget;
            
            if (system.pullPower != null)
                system.pullPower.ProcessInput(false, false, true); 

            PushableObject pushable = target.GetComponent<PushableObject>();
            if (pushable != null)
            {
                // Poprawka: Bezpośredni dostęp do kamery głównej
                Vector3 launchDir = Camera.main.transform.forward;
                pushable.ApplyPush(launchDir, launchForce);
            }
        }
    }

    void ExecuteSinglePush()
    {
        Transform rocket = system.GetCurrentTarget(system.projectileLayer);
        if (rocket) { Reflect(rocket); return; }

        Transform enemy = system.GetCurrentTarget(system.enemyLayer);
        if (enemy) 
        {
            Vector3 dirToEnemy = (enemy.position - transform.position).normalized;
            bool isLocked = (system.lockOn != null && system.lockOn.currentTarget == enemy);
            
            if (Vector3.Angle(transform.forward, dirToEnemy) < system.viewAngle / 1.5f || isLocked)
            {
                Push(enemy);
            }
        }
    }

    void ExecuteAoEPush()
    {
        Collider[] objects = Physics.OverlapSphere(transform.position, system.range);
        foreach (var obj in objects)
        {
            Vector3 dir = (obj.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) < system.viewAngle / 2f || Vector3.Distance(transform.position, obj.transform.position) < 2f)
            {
                if (((1 << obj.gameObject.layer) & system.projectileLayer) != 0) 
                    Reflect(obj.transform);
                else 
                    Push(obj.transform);
            }
        }
    }

    void Push(Transform t) 
    {
        Vector3 pushDir = (t.position - transform.position).normalized;
        t.GetComponent<PushableObject>()?.ApplyPush(pushDir, pushForce);
    }

    void Reflect(Transform t) => t.GetComponent<Projectile>()?.Reflect(transform.forward);
}