using UnityEngine;
using UnityEngine.InputSystem;

// A cone barrier that must be deliberately dragged out of the way with a key press.
public class MovableObstacle : MonoBehaviour
{
    public float interactRange = 2.2f;
    public Vector3 moveOffset = new Vector3(-1.0f, 0f, -1.6f);
    public float moveSpeed = 1.6f;
    public bool moved;

    private Transform player;
    private Vector3 closedPos;
    private Vector3 openPos;

    private void Awake()
    {
        closedPos = transform.position;
        openPos = closedPos + moveOffset;
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            player = playerGO.transform;
    }

    public bool PlayerInRange()
    {
        return player != null && Vector3.Distance(player.position, transform.position) <= interactRange;
    }

    public void ResetObstacle()
    {
        moved = false;
        transform.position = closedPos;
    }

    private void Update()
    {
        var target = moved ? openPos : closedPos;

        if ((transform.position - target).sqrMagnitude > 0.0001f)
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (GameInput.InteractPressed() && PlayerInRange())
            moved = !moved;
    }

    private void OnGUI()
    {
        if (StartScreen.IsShowing || GameOverScreen.IsShowing)
            return;

        if (!PlayerInRange())
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        string text = moved ? "[E] Put cone back" : "[E] Move cone aside";
        GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 30f), text, style);
    }
}
