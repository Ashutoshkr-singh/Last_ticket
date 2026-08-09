using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player open the nearest train door with a key press.
public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction")]
    public float interactRange = 4f;
    public bool showPrompt = true;

    private TrainDoor[] doors;

    private void Start()
    {
        doors = Object.FindObjectsByType<TrainDoor>(FindObjectsSortMode.None);
    }

    private TrainDoor FindNearestDoor()
    {
        if (doors == null)
            return null;

        TrainDoor nearest = null;
        float best = interactRange;

        foreach (var door in doors)
        {
            if (door == null)
                continue;

            float distance = Vector3.Distance(transform.position, door.transform.position);

            if (distance < best)
            {
                best = distance;
                nearest = door;
            }
        }

        return nearest;
    }

    private void Update()
    {
        if (!GameInput.InteractPressed())
            return;

        var door = FindNearestDoor();

        if (door != null)
            door.Toggle();
    }

    private void OnGUI()
    {
        if (!showPrompt)
            return;

        var door = FindNearestDoor();

        if (door == null)
            return;

        var style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 18;
        style.normal.textColor = Color.white;

        string text = door.isOpen ? "[E] Close door" : "[E] Open door";
        GUI.Label(new Rect(0f, Screen.height * 0.62f, Screen.width, 30f), text, style);
    }
}
