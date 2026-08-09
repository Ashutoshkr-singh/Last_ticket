using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Reusable Simon-Says style memory puzzle rendered on a World Space canvas.
// The grid is built at runtime so a kiosk only needs this one component.
public class PuzzleController : MonoBehaviour
{
    [Header("Puzzle")]
    public int minSequence = 3;
    public int maxSequence = 4;
    public int gridSize = 3;

    [Header("Timing")]
    public float startDelay = 0.5f;
    public float litDuration = 0.45f;
    public float gapDuration = 0.25f;

    [Header("Panel")]
    public Vector2 worldSize = new Vector2(0.75f, 0.75f);

    [Header("Events")]
    public UnityEvent onSolved;
    public UnityEvent onWrong;

    private Canvas canvas;
    private Image[] cells;
    private Text statusText;

    private readonly List<int> sequence = new List<int>();
    private int inputIndex;
    private bool acceptingInput;
    private bool isShowing;

    private static readonly Color IdleColor = new Color(0.10f, 0.12f, 0.16f, 0.95f);
    private static readonly Color LitColor = new Color(0.35f, 0.95f, 1f, 1f);
    private static readonly Color RightColor = new Color(0.25f, 0.95f, 0.35f, 1f);
    private static readonly Color WrongColor = new Color(0.95f, 0.20f, 0.20f, 1f);

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        BuildUI();
        Close();
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("PuzzleCanvas", typeof(RectTransform));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Authored at 300x300 px then scaled down to the requested world size, which
        // keeps font and spacing maths in comfortable integer pixels.
        const float refSize = 300f;
        var rt = canvasGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(refSize, refSize);
        rt.localScale = new Vector3(worldSize.x / refSize, worldSize.y / refSize, 1f);

        var bg = canvasGO.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.03f, 0.05f, 0.96f);

        var gridGO = new GameObject("Grid", typeof(RectTransform));
        gridGO.transform.SetParent(canvasGO.transform, false);
        var gridRT = gridGO.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0f, 0f);
        gridRT.anchorMax = new Vector2(1f, 1f);
        gridRT.offsetMin = new Vector2(12f, 12f);
        gridRT.offsetMax = new Vector2(-12f, -46f);

        var grid = gridGO.AddComponent<GridLayoutGroup>();
        float cell = (refSize - 24f - (gridSize - 1) * 8f) / gridSize;
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(8f, 8f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridSize;

        int count = gridSize * gridSize;
        cells = new Image[count];

        for (int i = 0; i < count; i++)
        {
            var cellGO = new GameObject("Cell" + i, typeof(RectTransform));
            cellGO.transform.SetParent(gridGO.transform, false);

            var img = cellGO.AddComponent<Image>();
            img.color = IdleColor;
            cells[i] = img;

            var button = cellGO.AddComponent<Button>();
            button.targetGraphic = img;

            int index = i;
            button.onClick.AddListener(() => OnCellClicked(index));
        }

        var textGO = new GameObject("Status", typeof(RectTransform));
        textGO.transform.SetParent(canvasGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0f, 1f);
        textRT.anchorMax = new Vector2(1f, 1f);
        textRT.pivot = new Vector2(0.5f, 1f);
        textRT.anchoredPosition = new Vector2(0f, -8f);
        textRT.sizeDelta = new Vector2(0f, 34f);

        statusText = textGO.AddComponent<Text>();
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 22;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.text = "WATCH";
    }

    public void Open()
    {
        // Without a world camera the GraphicRaycaster cannot hit-test a world space
        // canvas at all, so none of the cells would respond to clicks.
        if (canvas.worldCamera == null)
            canvas.worldCamera = Camera.main;

        IsOpen = true;
        canvas.gameObject.SetActive(true);

        // Select a cell so a gamepad can navigate the grid; without a selection the
        // UI module has nothing to move from and the pad does nothing.
        var eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null && cells.Length > 0)
            eventSystem.SetSelectedGameObject(cells[cells.Length / 2].gameObject);

        StartNewRound();
    }

    public void Close()
    {
        IsOpen = false;
        acceptingInput = false;
        StopAllCoroutines();

        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    public void StartNewRound()
    {
        StopAllCoroutines();
        StartCoroutine(ShowSequenceRoutine());
    }

    private IEnumerator ShowSequenceRoutine()
    {
        isShowing = true;
        acceptingInput = false;
        inputIndex = 0;

        sequence.Clear();
        int length = Random.Range(minSequence, maxSequence + 1);
        for (int i = 0; i < length; i++)
            sequence.Add(Random.Range(0, cells.Length));

        SetAllCells(IdleColor);
        statusText.text = "WATCH";

        yield return new WaitForSeconds(startDelay);

        foreach (int index in sequence)
        {
            cells[index].color = LitColor;
            yield return new WaitForSeconds(litDuration);
            cells[index].color = IdleColor;
            yield return new WaitForSeconds(gapDuration);
        }

        isShowing = false;
        acceptingInput = true;
        statusText.text = "REPEAT (" + sequence.Count + ")";
    }

    private void OnCellClicked(int index)
    {
        if (!acceptingInput || isShowing)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClick();

        if (sequence[inputIndex] == index)
        {
            inputIndex++;
            StartCoroutine(FlashCell(index, RightColor));

            if (inputIndex >= sequence.Count)
            {
                acceptingInput = false;
                statusText.text = "CORRECT";
                StartCoroutine(FinishRoutine(true));
            }
        }
        else
        {
            acceptingInput = false;
            statusText.text = "WRONG";
            StartCoroutine(FlashCell(index, WrongColor));
            StartCoroutine(FinishRoutine(false));
        }
    }

    private IEnumerator FlashCell(int index, Color color)
    {
        cells[index].color = color;
        yield return new WaitForSeconds(0.22f);

        if (cells[index] != null)
            cells[index].color = IdleColor;
    }

    private IEnumerator FinishRoutine(bool solved)
    {
        yield return new WaitForSeconds(0.6f);

        if (solved)
            onSolved.Invoke();
        else
            onWrong.Invoke();
    }

    private void SetAllCells(Color color)
    {
        foreach (var cell in cells)
            cell.color = color;
    }

    // Test hook: drives the puzzle without needing real UI clicks.
    public void DebugClickCell(int index)
    {
        OnCellClicked(index);
    }

    public IReadOnlyList<int> CurrentSequence
    {
        get { return sequence; }
    }

    public bool AcceptingInput
    {
        get { return acceptingInput; }
    }
}
