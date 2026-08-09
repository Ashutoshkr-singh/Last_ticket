using UnityEngine;

// A two leaf sliding train door. The leaf meshes keep their pivots at the model
// origin, so the leaves are driven by world position offsets rather than local ones.
public class TrainDoor : MonoBehaviour
{
    [Header("Leaves")]
    public Transform leafA;
    public Transform leafB;

    // A door leaf is not one mesh: the glass, rubber seals, glass frames and decals
    // are separate objects that must slide with their panel.
    public Transform[] leafAParts;
    public Transform[] leafBParts;

    [Header("Slide")]
    public Vector3 slideAxis = Vector3.forward;
    public float slideDistance = 0.78f;
    public float slideSpeed = 1.6f;

    [Header("State")]
    public bool isOpen;

    private Vector3 closedA;
    private Vector3 closedB;
    private Vector3[] closedAParts;
    private Vector3[] closedBParts;
    private float openAmount;

    private void Awake()
    {
        if (leafA != null) closedA = leafA.position;
        if (leafB != null) closedB = leafB.position;

        closedAParts = CapturePositions(leafAParts);
        closedBParts = CapturePositions(leafBParts);

        openAmount = isOpen ? 1f : 0f;
    }

    private static Vector3[] CapturePositions(Transform[] parts)
    {
        if (parts == null)
            return new Vector3[0];

        var positions = new Vector3[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            positions[i] = parts[i] != null ? parts[i].position : Vector3.zero;

        return positions;
    }

    private static void ApplyOffset(Transform[] parts, Vector3[] closed, Vector3 offset)
    {
        if (parts == null || closed == null)
            return;

        for (int i = 0; i < parts.Length && i < closed.Length; i++)
            if (parts[i] != null)
                parts[i].position = closed[i] + offset;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
    }

    private void Update()
    {
        float target = isOpen ? 1f : 0f;

        if (Mathf.Approximately(openAmount, target))
            return;

        openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime * slideSpeed);

        Vector3 offset = slideAxis.normalized * (slideDistance * openAmount);

        // leafA is the lower-coordinate leaf, so it retreats along -axis and leafB along +axis.
        if (leafA != null) leafA.position = closedA - offset;
        if (leafB != null) leafB.position = closedB + offset;

        ApplyOffset(leafAParts, closedAParts, -offset);
        ApplyOffset(leafBParts, closedBParts, offset);
    }
}
