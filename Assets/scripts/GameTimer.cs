using UnityEngine;

// Two minutes to reach the yellow seat. Once the ticket is in hand, loitering
// makes the clock burn down much faster.
public class GameTimer : MonoBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer")]
    public float totalTime = 120f;

    [Header("Idle Penalty")]
    public float idleThreshold = 2f;
    public float idleDrainMultiplier = 4f;
    public float idleMoveEpsilon = 0.06f;

    [Header("Goal")]
    public Transform goalSeat;
    public float goalRadius = 2f;

    private Transform player;
    private float remaining;
    private float idleTime;
    private Vector3 lastPosition;
    private bool running;
    private bool won;

    public float Remaining { get { return remaining; } }
    public bool IsDraining { get { return idleTime > idleThreshold; } }
    public bool Won { get { return won; } }

    private void Awake()
    {
        Instance = this;
        remaining = totalTime;
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            lastPosition = player.position;
        }

        running = true;
    }

    public void ResetTimer()
    {
        remaining = totalTime;
        idleTime = 0f;
        won = false;
        running = true;

        if (player != null)
            lastPosition = player.position;
    }

    private void Update()
    {
        if (!running || player == null)
            return;

        bool hasTicket = PlayerInventory.Instance != null && PlayerInventory.Instance.hasTicket;

        // Horizontal movement only, so looking around does not count as moving.
        Vector3 delta = player.position - lastPosition;
        delta.y = 0f;
        lastPosition = player.position;

        if (hasTicket && delta.magnitude < idleMoveEpsilon * Time.deltaTime * 60f)
            idleTime += Time.deltaTime;
        else
            idleTime = 0f;

        float drain = idleTime > idleThreshold ? idleDrainMultiplier : 1f;
        remaining -= Time.deltaTime * drain;

        if (goalSeat != null && Vector3.Distance(player.position, goalSeat.position) <= goalRadius)
        {
            won = true;
            running = false;
            return;
        }

        if (remaining <= 0f)
        {
            remaining = 0f;
            running = false;

            if (GameOverScreen.Instance != null)
                GameOverScreen.Instance.Show("Out of time - the train left without you");
        }
    }

    private void OnGUI()
    {
        // The scare owns the screen; the clock must not sit on top of it.
        if (StartScreen.IsShowing || GameOverScreen.IsShowing || Jumpscare.IsPlaying)
            return;

        int minutes = Mathf.FloorToInt(Mathf.Max(0f, remaining) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(0f, remaining) % 60f);
        string label = string.Format("{0:00}:{1:00}", minutes, seconds);

        var style = new GUIStyle();
        style.fontSize = 48;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleRight;

        var rect = new Rect(Screen.width - 250f, 18f, 230f, 60f);

        // Drop shadow keeps the digits readable against the bright platform lights.
        style.normal.textColor = new Color(0f, 0f, 0f, 0.65f);
        GUI.Label(new Rect(rect.x + 3f, rect.y + 3f, rect.width, rect.height), label, style);

        style.normal.textColor = IsDraining ? new Color(1f, 0.35f, 0.3f) : Color.white;
        GUI.Label(rect, label, style);

        if (IsDraining)
        {
            var warn = new GUIStyle();
            warn.fontSize = 18;
            warn.fontStyle = FontStyle.Bold;
            warn.alignment = TextAnchor.MiddleRight;
            warn.normal.textColor = new Color(1f, 0.35f, 0.3f);
            GUI.Label(new Rect(rect.x, rect.y + 56f, rect.width, 24f), "KEEP MOVING", warn);
        }

        if (won)
        {
            var win = new GUIStyle();
            win.fontSize = 40;
            win.fontStyle = FontStyle.Bold;
            win.alignment = TextAnchor.MiddleCenter;
            win.normal.textColor = new Color(0.3f, 1f, 0.4f);
            GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 60f), "Congratulations", win);

            var sub = new GUIStyle(win);
            sub.fontSize = 30;
            GUI.Label(new Rect(0f, Screen.height * 0.46f, Screen.width, 50f), "You are still meat", sub);
        }
    }
}
