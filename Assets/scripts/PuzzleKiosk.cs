using UnityEngine;
using UnityEngine.InputSystem;

// One kiosk station. Owns its own fail counter so the two kiosks never share strikes.
[RequireComponent(typeof(PuzzleController))]
public class PuzzleKiosk : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 2.5f;
    public string kioskName = "Kiosk";

    [Header("Rules")]
    public bool requiresTicket;
    public bool grantsTicket;
    public int maxFails = 2;

    [Header("On Solve")]
    public GameObject unlockTarget;   // disabled when solved (the barrier)
    public GameObject rewardProp;     // enabled when solved (the ticket)

    private PuzzleController puzzle;
    private Transform player;
    private PlayerMovement playerMovement;
    private int failCount;
    private bool solved;
    private bool scanned;
    private bool scanning;

    [Header("Scan")]
    public float scanDuration = 1.3f;

    private System.Collections.IEnumerator ScanRoutine()
    {
        scanning = true;
        yield return new WaitForSeconds(scanDuration);
        scanning = false;
        scanned = true;
        OpenPuzzle();
    }

    public int FailCount { get { return failCount; } }
    public bool Solved { get { return solved; } }

    private void Awake()
    {
        puzzle = GetComponent<PuzzleController>();
        puzzle.onSolved.AddListener(HandleSolved);
        puzzle.onWrong.AddListener(HandleWrong);
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerMovement = playerGO.GetComponent<PlayerMovement>();
        }

        if (rewardProp != null)
            rewardProp.SetActive(false);
    }

    public bool PlayerInRange()
    {
        return player != null && Vector3.Distance(player.position, transform.position) <= interactRange;
    }

    public bool CanInteract()
    {
        if (solved || puzzle.IsOpen)
            return false;

        if (requiresTicket && (PlayerInventory.Instance == null || !PlayerInventory.Instance.hasTicket))
            return false;

        return PlayerInRange();
    }

    public void OpenPuzzle()
    {
        puzzle.Open();

        // Disabling the movement script also releases the cursor (its OnDisable),
        // which is what lets the player actually click the world space buttons.
        if (playerMovement != null)
            playerMovement.enabled = false;
    }

    public void ClosePuzzle()
    {
        puzzle.Close();

        if (playerMovement != null)
            playerMovement.enabled = true;
    }

    private void HandleSolved()
    {
        solved = true;
        ClosePuzzle();

        if (grantsTicket && PlayerInventory.Instance != null)
            PlayerInventory.Instance.hasTicket = true;

        if (rewardProp != null)
            rewardProp.SetActive(true);

        if (unlockTarget != null)
            unlockTarget.SetActive(false);

        Debug.Log(kioskName + ": solved. hasTicket=" +
            (PlayerInventory.Instance != null && PlayerInventory.Instance.hasTicket));
    }

    private void HandleWrong()
    {
        failCount++;

        if (failCount >= maxFails)
        {
            ClosePuzzle();

            if (GameOverScreen.Instance != null)
                GameOverScreen.Instance.Show(kioskName + ": too many wrong patterns");

            return;
        }

        // First strike just reshuffles into a brand new pattern.
        puzzle.StartNewRound();
    }

    public void ResetKiosk()
    {
        failCount = 0;
        solved = false;
        scanned = false;
        scanning = false;
        ClosePuzzle();

        if (rewardProp != null)
        {
            var pickup = rewardProp.GetComponent<TicketPickup>();
            if (pickup != null)
                pickup.ResetPickup();

            rewardProp.SetActive(false);
        }

        if (unlockTarget != null)
            unlockTarget.SetActive(true);
    }

    private void Update()
    {
        // Back (or interact again) closes. Without this an accidental open leaves
        // the player with movement disabled and no way out.
        if (puzzle.IsOpen)
        {
            if (GameInput.BackPressed() || GameInput.InteractPressed())
                ClosePuzzle();

            return;
        }

        if (!GameInput.InteractPressed() || scanning || !CanInteract())
            return;

        // Ticket kiosks make the player scan first; the puzzle opens once it reads.
        if (requiresTicket && !scanned)
            StartCoroutine(ScanRoutine());
        else
            OpenPuzzle();
    }

    private void OnGUI()
    {
        if (StartScreen.IsShowing || GameOverScreen.IsShowing)
            return;

        if (player == null)
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        if (puzzle.IsOpen)
        {
            GUI.Label(new Rect(0f, Screen.height * 0.88f, Screen.width, 30f), "[Esc] Back", style);
            return;
        }

        if (scanning)
        {
            GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 30f), "SCANNING TICKET...", style);
            return;
        }

        if (solved || !PlayerInRange())
            return;

        bool locked = requiresTicket &&
            (PlayerInventory.Instance == null || !PlayerInventory.Instance.hasTicket);

        string text = locked ? "Need a ticket" : "[E] Use " + kioskName;
        GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 30f), text, style);
    }
}
