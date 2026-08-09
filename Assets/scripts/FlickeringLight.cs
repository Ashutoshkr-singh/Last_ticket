using UnityEngine;

[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    [Header("Intensity Range")]
    // Zero, not a dim floor: when a lamp drops out it must go completely dark.
    public float minIntensity = 0f;
    public float maxIntensity = 4.5f;

    [Header("Steady")]
    // Level held whenever the timer is running normally.
    public float steadyIntensity = 2.6f;

    [Header("Flicker")]
    public float flickerSpeed = 3.5f;
    public float smoothing = 20f;

    [Header("Blackouts")]
    [Range(0f, 1f)] public float blackoutChancePerSecond = 0.25f;
    public float minBlackoutDuration = 0.15f;
    public float maxBlackoutDuration = 1.1f;

    private new Light light;
    private float noiseSeed;
    private float blackoutTimer;

    private void Awake()
    {
        light = GetComponent<Light>();

        // Each lamp gets its own noise offset, otherwise they all flicker in unison
        // and it reads as a global fade instead of individual failing tubes.
        noiseSeed = Random.value * 1000f;
    }

    private void Update()
    {
        // Lamps only misbehave while the clock is burning down fast. The rest of the
        // time they hold a steady level so the station reads as normal.
        bool panic = GameTimer.Instance != null && GameTimer.Instance.IsDraining;

        if (!panic)
        {
            blackoutTimer = 0f;
            light.intensity = Mathf.Lerp(light.intensity, steadyIntensity, Time.deltaTime * smoothing);
            return;
        }

        float target;

        if (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.deltaTime;
            target = minIntensity;
        }
        else
        {
            if (Random.value < blackoutChancePerSecond * Time.deltaTime)
            {
                blackoutTimer = Random.Range(minBlackoutDuration, maxBlackoutDuration);
            }

            float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);

            // Stretched around the midpoint so the lamp sweeps the whole range: it
            // reaches true black at the bottom and full brightness at the top, while
            // still passing smoothly through the middle rather than snapping.
            noise = Mathf.Clamp01((noise - 0.5f) * 2.6f + 0.5f);

            target = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }

        light.intensity = Mathf.Lerp(light.intensity, target, Time.deltaTime * smoothing);
    }
}
