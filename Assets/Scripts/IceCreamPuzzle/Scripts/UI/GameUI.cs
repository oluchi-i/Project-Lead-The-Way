using UnityEngine;
using UnityEngine.UI;

// Attach to any GameObject that already has (or will receive) a Canvas component.
// Everything is built in code — no prefabs required.

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
[RequireComponent(typeof(GraphicRaycaster))]
public class GameUI : MonoBehaviour
{
    // ── Palette ───────────────────────────────────────────────────────────────
    static readonly Color C_HUD  = new Color(1.00f, 0.92f, 0.88f, 0.95f);
    static readonly Color C_WIN  = new Color(0.45f, 0.91f, 0.60f, 0.94f);
    static readonly Color C_LOSE = new Color(0.95f, 0.58f, 0.63f, 0.94f);
    static readonly Color C_TXT  = new Color(0.20f, 0.13f, 0.10f, 1.00f);
    static readonly Color C_BTN  = new Color(0.98f, 0.82f, 0.47f, 1.00f);
    static readonly Color C_STP  = new Color(0.49f, 0.87f, 0.69f, 1.00f);

    // ── Live labels ───────────────────────────────────────────────────────────
    Text _moveLabel;
    Text _stepLabel;
    Text _hintLabel;

    GameObject _winPanel;
    Text       _winBody;
    GameObject _losePanel;

    Font _font;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        // Configure canvas
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight   = 0.5f;

        // Load built-in font — try both names used across Unity versions
        _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_font == null)
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildHUD();
        BuildStepButton();
        BuildWinPanel();
        BuildLosePanel();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void UpdateMoveCount(int n)  { if (_moveLabel) _moveLabel.text = "Moves: " + n; }
    public void UpdateStepCount(int n)  { if (_stepLabel) _stepLabel.text = "Steps: " + n; }

    public void UpdateState(GameState state)
    {
        if (!_hintLabel) return;
        if      (state == GameState.WaitingInput) _hintLabel.text = "Click obstacle  →  WASD to slide  |  SPACE to step  |  R to restart";
        else if (state == GameState.Stepping)     _hintLabel.text = "Moving…";
        else if (state == GameState.Won)          _hintLabel.text = "Level complete!";
        else if (state == GameState.Lost)         _hintLabel.text = "Stuck! Press R to restart.";
        else                                      _hintLabel.text = "";
    }

    public void ShowWin(int steps, int moves)
    {
        if (_winPanel)  _winPanel.SetActive(true);
        if (_winBody)   _winBody.text = "Steps: " + steps + "    Obstacle moves: " + moves + "\n\nPress R to play again";
    }

    public void ShowLost() { if (_losePanel) _losePanel.SetActive(true); }

    // ── Builders ──────────────────────────────────────────────────────────────

    void BuildHUD()
    {
        // Top bar
        GameObject bar = Panel("HUD", transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 64));
        bar.GetComponent<Image>().color = C_HUD;

        Txt("Title", bar.transform, "Lead the Way",
            28, true, C_TXT, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(500, 60));

        _moveLabel = Txt("Moves", bar.transform, "Moves: 0",
            22, false, C_TXT, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-190, 0), new Vector2(170, 60));

        _stepLabel = Txt("Steps", bar.transform, "Steps: 0",
            22, false, C_TXT, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(160, 60));

        // Hint bar (sits just below the HUD)
        GameObject hintGO = Panel("HintBar", transform,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, 30));
        hintGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        hintGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -64);

        _hintLabel = Txt("Hint", hintGO.transform,
            "Click obstacle  ->  WASD to slide  |  SPACE to step",
            16, false, new Color(0.35f, 0.20f, 0.14f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 30));
        _hintLabel.alignment = TextAnchor.MiddleCenter;
    }

    void BuildStepButton()
    {
        GameObject go = Panel("StepBtn", transform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(180, 54));
        go.GetComponent<Image>().color = C_STP;
        go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 22);

        Txt("L", go.transform, "STEP", 24, true, new Color(0.1f, 0.3f, 0.2f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 54))
            .alignment = TextAnchor.MiddleCenter;

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(OnStepClicked);
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
    }

    void OnStepClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AdvanceCharacter();
    }

    void BuildWinPanel()
    {
        _winPanel = Panel("WinPanel", transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460, 300));
        _winPanel.GetComponent<Image>().color = C_WIN;
        _winPanel.SetActive(false);

        Txt("T", _winPanel.transform, "You Win!",
            44, true, C_TXT, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(440, 56))
            .alignment = TextAnchor.MiddleCenter;

        _winBody = Txt("B", _winPanel.transform, "",
            20, false, C_TXT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(420, 100));
        _winBody.alignment  = TextAnchor.MiddleCenter;
        _winBody.lineSpacing = 1.4f;

        PanelBtn(_winPanel.transform, "Play Again", new Vector2(0, -106), OnRestart);
    }

    void BuildLosePanel()
    {
        _losePanel = Panel("LosePanel", transform,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420, 240));
        _losePanel.GetComponent<Image>().color = C_LOSE;
        _losePanel.SetActive(false);

        Txt("T", _losePanel.transform, "Stuck!",
            44, true, C_TXT, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(400, 56))
            .alignment = TextAnchor.MiddleCenter;

        Txt("B", _losePanel.transform, "The character is surrounded.\nRearrange obstacles first.",
            19, false, C_TXT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 18), new Vector2(390, 80))
            .alignment = TextAnchor.MiddleCenter;

        PanelBtn(_losePanel.transform, "Try Again", new Vector2(0, -78), OnRestart);
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartLevel();
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    GameObject Panel(string id, Transform parent,
        Vector2 aMin, Vector2 aMax, Vector2 piv, Vector2 size)
    {
        GameObject go = new GameObject(id);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>();
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = piv;
        rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;
        return go;
    }

    Text Txt(string id, Transform parent, string content,
        int size, bool bold, Color col,
        Vector2 anchor, Vector2 piv, Vector2 offset, Vector2 dim)
    {
        GameObject go = new GameObject(id);
        go.AddComponent<RectTransform>();
        go.AddComponent<Text>();
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = piv; rt.sizeDelta = dim; rt.anchoredPosition = offset;

        Text t = go.GetComponent<Text>();
        t.text      = content;
        t.font      = _font;            // null-safe: Unity Text uses built-in fallback
        t.fontSize  = size;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color     = col;
        t.alignment = TextAnchor.MiddleLeft;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        return t;
    }

    void PanelBtn(Transform parent, string label, Vector2 offset,
        UnityEngine.Events.UnityAction cb)
    {
        GameObject go = Panel("Btn_" + label, parent,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(200, 50));
        go.GetComponent<Image>().color = C_BTN;
        go.GetComponent<RectTransform>().anchoredPosition = offset;

        Txt("L", go.transform, label, 22, true, new Color(0.2f, 0.1f, 0f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 50))
            .alignment = TextAnchor.MiddleCenter;

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(cb);
        Navigation nav = btn.navigation;
        nav.mode = Navigation.Mode.None;
        btn.navigation = nav;
    }
}
