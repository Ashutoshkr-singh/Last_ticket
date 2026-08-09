using UnityEngine;
using UnityEngine.UI;

// Game over panel. "Restart" puts the player back at the spawn point and clears
// all puzzle progress, rather than reloading the scene.
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen Instance { get; private set; }

    private Canvas canvas;
    private Text messageText;

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
        canvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel", typeof(RectTransform));
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRT = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.86f);

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
    }

    public void Show(string reason)
    {
        if (messageText != null)
            messageText.text = reason;

        canvas.gameObject.SetActive(true);

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
