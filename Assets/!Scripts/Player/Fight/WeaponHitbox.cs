using UnityEngine;
using System.Collections.Generic;
using System.Collections; // Potrzebne do korutyn

public class WeaponHitbox : MonoBehaviour
{
    public int damage = 10;
    public Collider weaponCollider;
    public LayerMask targetLayer; 
    
    [Header("Hit-Stop Settings")]
    public float hitStopDuration = 0.07f; // Jak długo czas ma stać
    public float hitStopTimeScale = 0.05f; // Do jakiej wartości zwalniamy

    private List<GameObject> hitObjects = new List<GameObject>();

    void Start()
    {
        if (weaponCollider == null) weaponCollider = GetComponent<Collider>();
        weaponCollider.enabled = false;
        weaponCollider.isTrigger = true;
    }

    public void StartAttack()
    {
        hitObjects.Clear();
        weaponCollider.enabled = true;
    }

    public void StopAttack() => weaponCollider.enabled = false;

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            if (!hitObjects.Contains(other.gameObject))
            {
                // HIT-STOP: Wywołujemy efekt przy trafieniu
                StopAllCoroutines();
                StartCoroutine(DoHitStop());

                // Obrażenia
                other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                hitObjects.Add(other.gameObject);

                // Debug log
                string attackerName = GetComponentInParent<PlayerCombat>() != null ? "Gracz" : "Przeciwnik";
                Debug.Log($"<color=red>TRAFIONO {other.name} przez {attackerName}! (HIT-STOP)</color>");
            }
        }
    }

    IEnumerator DoHitStop()
    {
        float originalTimeScale = 1f;
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = originalTimeScale;
    }
}