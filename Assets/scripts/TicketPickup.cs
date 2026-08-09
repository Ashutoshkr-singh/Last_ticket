using UnityEngine;
using UnityEngine.InputSystem;

// The printed ticket sitting in the machine. Collecting it is what actually grants
// the ticket, so the player has to take it rather than just solving the puzzle.
public class TicketPickup : MonoBehaviour
{
    public float pickupRange = 2.5f;
    public float spinSpeed = 45f;
    public bool collected;

    private Transform player;

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;
    }

    public bool PlayerInRange()
    {
        return !collected && player != null &&
            Vector3.Distance(player.position, transform.position) <= pickupRange;
    }

    private void Update()
    {
        if (collected)
            return;

        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (!GameInput.InteractPressed() || !PlayerInRange())
            return;

        Collect();
    }

    public void Collect()
    {
        collected = true;

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.hasTicket = true;

        gameObject.SetActive(false);
        Debug.Log("Ticket collected. hasTicket=true");
    }

    public void ResetPickup()
    {
        collected = false;
    }

    private void OnGUI()
    {
        if (!PlayerInRange())
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 30f), "[E] Take ticket", style);
    }
}
