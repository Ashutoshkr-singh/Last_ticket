using UnityEngine;
using UnityEngine.UI;

// Title card shown before the run begins. Holds the game frozen until PLAY GAME.
public class StartScreen : MonoBehaviour
{
    public static bool IsShowing { get; private set; }

    public Sprite titleImage;

    private Canvas canvas;

    private void Awake()
    {
        BuildUI();
    }

    private void Start()
    {
        Show();
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("StartCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Backdrop", typeof(RectTransform));
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f);

        var artGO = new GameObject("Title", typeof(RectTransform));
        artGO.transform.SetParent(canvasGO.transform, false);
        var artRT = artGO.GetComponent<RectTransform>();
        artRT.anchorMin = new Vector2(0.5f, 0.5f);
        artRT.anchorMax = new Vector2(0.5f, 0.5f);
        artRT.anchoredPosition = new Vector2(0f, 0f);
        artRT.sizeDelta = new Vector2(820f, 735f);

        var art = artGO.AddComponent<Image>();
        art.preserveAspect = true;
        art.sprite = titleImage;

        var buttonGO = new GameObject("PlayButton", typeof(RectTransform));
        buttonGO.transform.SetParent(canvasGO.transform, false);
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        // Sits over the "PLAY GAME" text baked into the artwork. Generous hit area so
        // it does not require pinpointing the glyphs.
        buttonRT.anchoredPosition = new Vector2(0f, -212f);
        buttonRT.sizeDelta = new Vector2(440f, 96f);

        var buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = Color.white;
        buttonImage.raycastTarget = true;

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // Invisible at rest, but lights up under the cursor so it reads as a button.
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.20f);
        colors.pressedColor = new Color(1f, 0.3f, 0.3f, 0.38f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0.12f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;

        button.onClick.AddListener(PlayGame);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 28;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 1f, 1f, 0f);
        label.raycastTarget = false;   // must not swallow clicks meant for the button
        label.text = "PLAY GAME";
    }

    public void Show()
    {
        IsShowing = true;
        canvas.gameObject.SetActive(true);

        // Freeze the run so the timer does not burn down behind the title card.
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerActive(false);
    }

    public void PlayGame()
    {
        IsShowing = false;
        canvas.gameObject.SetActive(false);

        Time.timeScale = 1f;

        SetPlayerActive(true);

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();
    }

    private void SetPlayerActive(bool active)
    {
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
            return;

        var movement = playerGO.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = active;
    }
}
