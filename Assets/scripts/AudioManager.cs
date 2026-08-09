using System.Collections;
using UnityEngine;

// Central audio. Footsteps and the ghost ambience are driven here by polling so
// the gameplay scripts only need to fire one-shots.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Clips")]
    public AudioClip footsteps;      // 5.17s loop
    public AudioClip ghostBreath;    // 66.04s
    public AudioClip uiClick;        // 0.41s
    public AudioClip doorChime;      // 3.14s
    public AudioClip jumpscare;      // 11.28s
    public AudioClip darkImpact;     // 22.01s
    public AudioClip backgroundMusic;

    [Header("Levels")]
    public float footstepVolume = 0.45f;
    public float ghostVolume = 0.55f;
    public float sfxVolume = 0.8f;
    public float musicVolume = 0.22f;

    [Header("Footsteps")]
    public float walkPitch = 0.78f;
    public float runPitch = 1.18f;

    [Header("Ghost Breath")]
    public float minGapAfterBreath = 18f;
    public float maxGapAfterBreath = 34f;

    private AudioSource footstepSource;
    private AudioSource ambientSource;
    private AudioSource sfxSource;
    private AudioSource musicSource;

    private PlayerMovement player;
    private CharacterController controller;
    private bool impactPlayed;
    private Vector3 lastPlayerPos;

    private void Awake()
    {
        Instance = this;

        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.clip = footsteps;
        footstepSource.loop = true;
        footstepSource.playOnAwake = false;
        footstepSource.volume = footstepVolume;
        footstepSource.spatialBlend = 0f;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = false;
        ambientSource.playOnAwake = false;
        ambientSource.volume = ghostVolume;
        ambientSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
        sfxSource.spatialBlend = 0f;

        // Continuous score for the whole run, sitting under everything else.
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = backgroundMusic;
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f;
    }

    private void Start()
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.GetComponent<PlayerMovement>();
            controller = playerGO.GetComponent<CharacterController>();
            lastPlayerPos = playerGO.transform.position;
        }

        // Started here, not in Awake: a streamed clip is not ready that early and
        // Play() would silently do nothing.
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        if (ghostBreath != null)
            StartCoroutine(GhostBreathRoutine());
    }

    // One breath episode, then silence, so it reads as intervals rather than a bed.
    private IEnumerator GhostBreathRoutine()
    {
        yield return new WaitForSeconds(Random.Range(8f, 16f));

        while (true)
        {
            ambientSource.PlayOneShot(ghostBreath, ghostVolume);
            yield return new WaitForSeconds(ghostBreath.length + Random.Range(minGapAfterBreath, maxGapAfterBreath));
        }
    }

    public void PlayClick()
    {
        if (uiClick != null)
            sfxSource.PlayOneShot(uiClick, sfxVolume);
    }

    public void PlayDoorChime()
    {
        if (doorChime != null)
            sfxSource.PlayOneShot(doorChime, sfxVolume * 0.8f);
    }

    public void PlayJumpscare()
    {
        if (jumpscare != null)
            sfxSource.PlayOneShot(jumpscare, sfxVolume);
    }

    private void Update()
    {
        UpdateFootsteps();
        UpdateDoomImpact();
    }

    private void UpdateFootsteps()
    {
        if (controller == null || player == null || footsteps == null)
            return;

        // Measured from the transform rather than controller.velocity, which is only
        // populated by per-frame Move calls and reads zero in some cases.
        Vector3 flat = player.transform.position - lastPlayerPos;
        flat.y = 0f;
        lastPlayerPos = player.transform.position;

        float speed = Time.deltaTime > 0f ? flat.magnitude / Time.deltaTime : 0f;

        // A teleport (respawn) must not register as a footstep burst.
        if (flat.magnitude > 2f)
            speed = 0f;

        bool walking = controller.isGrounded && speed > 0.35f && player.enabled;

        if (walking)
        {
            footstepSource.pitch = player.IsRunning ? runPitch : walkPitch;

            if (!footstepSource.isPlaying)
                footstepSource.Play();
        }
        else if (footstepSource.isPlaying)
        {
            footstepSource.Pause();
        }
    }

    // The impact clip is 22s long, so starting it with 22s left lands its peak
    // right as the timer expires.
    private void UpdateDoomImpact()
    {
        if (darkImpact == null || GameTimer.Instance == null)
            return;

        float remaining = GameTimer.Instance.Remaining;

        if (remaining > darkImpact.length + 0.5f)
        {
            impactPlayed = false;
            return;
        }

        if (!impactPlayed && remaining > 0f && !GameTimer.Instance.Won)
        {
            impactPlayed = true;
            sfxSource.PlayOneShot(darkImpact, sfxVolume * 0.85f);
        }
    }
}
