using UnityEngine;

// Pulls the train out of the station along the track when the run ends.
public class TrainDeparture : MonoBehaviour
{
    public Transform[] carriages;
    public Vector3 direction = Vector3.forward;
    public float acceleration = 4f;
    public float maxSpeed = 22f;

    private Vector3[] startPositions;
    private bool departing;
    private float speed;

    private void Awake()
    {
        CaptureStart();
    }

    private void CaptureStart()
    {
        if (carriages == null)
            return;

        startPositions = new Vector3[carriages.Length];
        for (int i = 0; i < carriages.Length; i++)
            startPositions[i] = carriages[i] != null ? carriages[i].position : Vector3.zero;
    }

    public void Depart()
    {
        departing = true;
    }

    public void ResetTrain()
    {
        departing = false;
        speed = 0f;

        if (carriages == null || startPositions == null)
            return;

        for (int i = 0; i < carriages.Length && i < startPositions.Length; i++)
            if (carriages[i] != null)
                carriages[i].position = startPositions[i];
    }

    private void Update()
    {
        if (!departing || carriages == null)
            return;

        speed = Mathf.Min(maxSpeed, speed + acceleration * Time.deltaTime);
        Vector3 step = direction.normalized * (speed * Time.deltaTime);

        foreach (var carriage in carriages)
            if (carriage != null)
                carriage.position += step;
    }
}
