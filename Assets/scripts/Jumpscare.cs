using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Fullscreen face + camera shake + scream. Runs before the Game Over panel.
public class Jumpscare : MonoBehaviour
{
    public static Jumpscare Instance { get; private set; }
    public static bool IsPlaying { get; private set; }

    [Header("Visual")]
    public Sprite face;
    public float duration = 1.5f;
    public float punchInScale = 1.25f;

    [Header("Shake")]
    public float shakeAmplitude = 0.22f;
    public float shakeFrequency = 42f;
    public float rollAmplitude = 5.5f;

    private Canvas canvas;
    private Image image;

    private void Awake()
    {
        Instance = this;
        BuildUI();
        canvas.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("JumpscareCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;   // above the game over panel and the title card

        var bgGO = new GameObject("Backdrop", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = Color.black;

        var faceGO = new GameObject("Face", typeof(RectTransform));
        faceGO.transform.SetParent(canvasGO.transform, false);
        // Stretched to the whole screen with preserveAspect, so the face fills the
        // frame at any resolution instead of sitting at a fixed pixel size.
        var faceRT = faceGO.GetComponent<RectTransform>();
        faceRT.anchorMin = Vector2.zero;
        faceRT.anchorMax = Vector2.one;
        faceRT.offsetMin = Vector2.zero;
        faceRT.offsetMax = Vector2.zero;

        image = faceGO.AddComponent<Image>();
        image.preserveAspect = true;
        image.sprite = face;
    }

    public void Play()
    {
        if (face != null)
            image.sprite = face;

        StopAllCoroutines();
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        IsPlaying = true;
        canvas.gameObject.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJumpscare();

        var cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 camBase = cam != null ? cam.localPosition : Vector3.zero;
        Quaternion rotBase = cam != null ? cam.localRotation : Quaternion.identity;

        var faceRT = image.rectTransform;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float falloff = 1f - t;

            if (cam != null)
            {
                // Trig-driven rather than random so the shake reads as a violent
                // rattle instead of noise, and decays as the scare settles.
                float sx = Mathf.Sin(elapsed * shakeFrequency) * shakeAmplitude * falloff;
                float sy = Mathf.Cos(elapsed * shakeFrequency * 1.37f) * shakeAmplitude * falloff;
                cam.localPosition = camBase + new Vector3(sx, sy, 0f);
                cam.localRotation = rotBase * Quaternion.Euler(0f, 0f,
                    Mathf.Sin(elapsed * shakeFrequency * 0.8f) * rollAmplitude * falloff);
            }

            // Lunge towards the player.
            float scale = Mathf.Lerp(1f, punchInScale, t);
            faceRT.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        if (cam != null)
        {
            cam.localPosition = camBase;
            cam.localRotation = rotBase;
        }

        faceRT.localScale = Vector3.one;
        canvas.gameObject.SetActive(false);
        IsPlaying = false;
    }

    public void Hide()
    {
        StopAllCoroutines();
        canvas.gameObject.SetActive(false);
        IsPlaying = false;
    }
}
