using UnityEngine;
using UnityEngine.UI;

// Game over panel. "Restart" puts the player back at the spawn point and clears
// all puzzle progress, rather than reloading the scene.
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }
    public static bool IsShowing { get; private set; }

    public Sprite endScreen;

    private Canvas canvas;
    private Text messageText;
    private Image endArt;

    private void Awake()
    {
        Instance = this;
        BuildUI();
        canvas.gameObject.SetActive(false);
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("GameOverCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        // Fully opaque: a translucent panel let the lit station read through the art.
        panelGO.AddComponent<Image>().color = Color.black;

        // The end screen artwork covers the panel and is itself the restart button,
        // so clicking anywhere on it starts a new run.
        var artGO = new GameObject("EndArt", typeof(RectTransform));
        artGO.transform.SetParent(panelGO.transform, false);
        var artRT = artGO.GetComponent<RectTransform>();
        artRT.anchorMin = Vector2.zero;
        artRT.anchorMax = Vector2.one;
        artRT.offsetMin = Vector2.zero;
        artRT.offsetMax = Vector2.zero;

        endArt = artGO.AddComponent<Image>();
        endArt.preserveAspect = true;
        endArt.sprite = endScreen;
        endArt.raycastTarget = true;

        var artButton = artGO.AddComponent<Button>();
        artButton.targetGraphic = endArt;
        var artColors = artButton.colors;
        artColors.normalColor = Color.white;
        artColors.highlightedColor = new Color(1f, 0.85f, 0.85f);
        artColors.pressedColor = new Color(1f, 0.55f, 0.55f);
        artColors.fadeDuration = 0.1f;
        artButton.colors = artColors;
        artButton.onClick.AddListener(Restart);

        var titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.5f);
        titleRT.anchorMax = new Vector2(0.5f, 0.5f);
        titleRT.anchoredPosition = new Vector2(0f, 70f);
        titleRT.sizeDelta = new Vector2(700f, 90f);

        var title = titleGO.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 54;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.9f, 0.15f, 0.15f);
        title.text = "GAME OVER";

        var msgGO = new GameObject("Message", typeof(RectTransform));
        msgGO.transform.SetParent(panelGO.transform, false);
        var msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.5f, 0.5f);
        msgRT.anchorMax = new Vector2(0.5f, 0.5f);
        msgRT.anchoredPosition = new Vector2(0f, 10f);
        msgRT.sizeDelta = new Vector2(700f, 50f);

        messageText = msgGO.AddComponent<Text>();
        messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        messageText.fontSize = 22;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.color = Color.white;

        var buttonGO = new GameObject("RestartButton", typeof(RectTransform));
        buttonGO.transform.SetParent(panelGO.transform, false);
        var buttonRT = buttonGO.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRT.anchoredPosition = new Vector2(0f, -70f);
        buttonRT.sizeDelta = new Vector2(240f, 60f);

        var buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.16f, 0.18f, 0.24f);

        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Restart);

        var labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;

        var label = labelGO.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 26;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "RESTART";

        // When artwork is supplied it stands in for the whole text panel.
        if (endScreen != null)
        {
            titleGO.SetActive(false);
            msgGO.SetActive(false);
            buttonGO.SetActive(false);
        }
    }

    public void Show(string reason)
    {
        if (messageText != null)
            messageText.text = reason;

        // Jumpscare first (it plays the scream itself), panel lands underneath it.
        if (Jumpscare.Instance != null)
            Jumpscare.Instance.Play();
        else if (AudioManager.Instance != null)
            AudioManager.Instance.PlayJumpscare();

        canvas.gameObject.SetActive(true);
        IsShowing = true;

        // The train pulls out of the station as the run ends.
        var departure = Object.FindFirstObjectByType<TrainDeparture>();
        if (departure != null)
            departure.Depart();

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
        {
            var movement = playerGO.GetComponent<PlayerMovement>();
            if (movement != null)
                movement.enabled = false;   // also frees the cursor for the button
        }
    }

    public void Restart()
    {
        canvas.gameObject.SetActive(false);
        IsShowing = false;

        if (Jumpscare.Instance != null)
            Jumpscare.Instance.Hide();

        foreach (var kiosk in Object.FindObjectsByType<PuzzleKiosk>(FindObjectsSortMode.None))
            kiosk.ResetKiosk();

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.ResetInventory();

        var departure = Object.FindFirstObjectByType<TrainDeparture>();
        if (departure != null)
            departure.ResetTrain();

        foreach (var cone in Object.FindObjectsByType<MovableObstacle>(FindObjectsSortMode.None))
            cone.ResetObstacle();

        if (GameTimer.Instance != null)
            GameTimer.Instance.ResetTimer();

        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO == null)
            return;

        var respawn = playerGO.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.Respawn();

        var movement = playerGO.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = true;
            movement.SyncLookToTransform();
        }
    }
}
