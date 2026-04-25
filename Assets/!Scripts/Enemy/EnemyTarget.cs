using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    public GameObject lockOnVisual; // Tu przeciągniesz kółko (Sprite)

    void Start() { if (lockOnVisual != null) lockOnVisual.SetActive(false); }

    public void ToggleIndicator(bool isActive)
    {
        if (lockOnVisual != null) lockOnVisual.SetActive(isActive);
    }
}