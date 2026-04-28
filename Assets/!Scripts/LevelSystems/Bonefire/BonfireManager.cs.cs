using System.Collections.Generic;
using UnityEngine;

public class BonfireManager : MonoBehaviour
{
    public static BonfireManager Instance;

    // Lista wszystkich odkrytych ognisk
    public List<BonfireInteraction> discoveredBonfires = new List<BonfireInteraction>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Metoda wywoływana, gdy gracz pierwszy raz użyje ogniska
public void RegisterBonfire(BonfireInteraction bonfire)
{
    if (!discoveredBonfires.Contains(bonfire))
    {
        discoveredBonfires.Add(bonfire);
        Debug.Log("DODANO OGNISKO: " + bonfire.bonfireName); // To musi się pojawić w konsoli!
    }
}
}