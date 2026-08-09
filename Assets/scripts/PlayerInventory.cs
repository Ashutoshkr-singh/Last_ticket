using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public bool hasTicket;

    private void Awake()
    {
        Instance = this;
    }

    public void ResetInventory()
    {
        hasTicket = false;
    }
}
