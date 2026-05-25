using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Minispiel 09 – Wahr oder Falsch: Ist die Aussage richtig oder falsch?
/// Lustige/absurde Fakten erscheinen. Spieler tippen WAHR oder FALSCH.
/// (+1 richtig, -1 falsch, sofort nächste Frage nach Klick)
/// Fragen stammen aus Assets/StreamingAssets/minispiel09_fragen.json
/// Seed-RPC-Pattern: MasterClient sendet Seed → gleiche Fragereihenfolge auf allen Clients.
/// </summary>
public class Minispiel09 : MinigameBase
{
    // ------------------------------------------------------------------ Layout
    private const float ButtonWidth    = 240f;
    private const float ButtonHeight   = 70f;
    private const float ButtonHalfGap  = 130f;   // Abstand Mitte→Button-Mittelpunkt
    private const float BottomOffset   = 60f;     // Pflichtabstand (Balken unten)
    private const float QuestionFontSize = 32f;
    private const float QuestionHeight = 170f;
    private const float ScoreFontSize  = 28f;
    private const float ScoreHeight    = 40f;

    // Y-Positionen (Ankerpunkt = Panel-Unterkante, pivot.y = 0)
    private static float Row1Y      => BottomOffset;                              // 60
    private static float QuestionY  => Row1Y + ButtonHeight + 20f;               // 150
    private static float ScoreY     => QuestionY + QuestionHeight + 14f;         // 334

    // ------------------------------------------------------------------ Farben
    private static readonly Color WahrColor     = new Color(0.13f, 0.70f, 0.22f);  // Grün
    private static readonly Color FalschColor   = new Color(0.82f, 0.16f, 0.16f);  // Rot
    private static readonly Color WahrHighlight = new Color(0.20f, 0.85f, 0.30f);
    private static readonly Color WahrPressed   = new Color(0.08f, 0.50f, 0.15f);
    private static readonly Color FalschHighlight = new Color(0.95f, 0.25f, 0.25f);
    private static readonly Color FalschPressed   = new Color(0.60f, 0.10f, 0.10f);
    private static readonly Color BtnDisabled   = new Color(0.30f, 0.30f, 0.30f, 0.50f);

    // ------------------------------------------------------------------ Datenmodell
    [Serializable]
    private class Frage
    {
        public string aussage;
        public bool   antwort;
    }

    [Serializable]
    private class Fragendatenbank
    {
        public Frage[] fragen;
    }

    // ------------------------------------------------------------------ State
    private int            localScore;
    private int            currentIndex;
    private int            randomSeed;
    private List<Frage>    questions;

    // ------------------------------------------------------------------ UI
    private bool           uiBuilt;
    private RectTransform  gameContainer;
    private TMP_Text       questionText;
    private TMP_Text       scoreText;
    private Button         wahrButton;
    private Button         falschButton;

    // ================================================================== Seed-RPC (vor TriggerMinigameStart)

    [PunRPC]
    public void RpcSetSeed(int seed)
    {
        randomSeed = seed;
    }

    // Wird vom GameRoomManager (oder einem anderen MasterClient-Aufruf) verwendet,
    // um Seed zu senden UND das Spiel gleichzeitig zu starten.
    // Da TriggerMinigameStart() nicht virtual ist, überschreiben wir es per 'new'.
    public override void TriggerMinigameStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int seed = UnityEngine.Random.Range(0, 99999);
            photonView.RPC(nameof(RpcSetSeed), RpcTarget.All, seed);
        }
        base.TriggerMinigameStart();
    }

    // ================================================================== MinigameBase-Implementierung

    protected override void SetupGame()
    {
        countdownTime = 30f;
        localScore    = 0;
        currentIndex  = 0;

        LoadQuestions();
        ShuffleQuestions();
        HideLegacyElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetButtonsInteractable(false);
        UpdateScoreDisplay();
        ShowCurrentQuestion();
    }

    protected override void StartActualGame()
    {
        SetButtonsInteractable(true);
        if (infoText != null)
            infoText.text = "Stimmt die Aussage? Tippe WAHR oder FALSCH!";
    }

    protected override void EndActualGame()
    {
        SetButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    protected override float GetLocalPlayerScore() => localScore;

    // ================================================================== Fragen laden & mischen

    private void LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "minispiel09_fragen.json");
        try
        {
            string json = File.ReadAllText(path);
            var db = JsonUtility.FromJson<Fragendatenbank>(json);
            questions = new List<Frage>(db.fragen);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Minispiel09] Fehler beim Laden der Fragen: {e.Message}");
            questions = new List<Frage>
            {
                new Frage { aussage = "Die Erde ist rund.", antwort = true  },
                new Frage { aussage = "Das Wasser ist trocken.",  antwort = false }
            };
        }
    }

    private void ShuffleQuestions()
    {
        if (questions == null || questions.Count == 0) return;
        var rng = new System.Random(randomSeed);
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j       = rng.Next(0, i + 1);
            Frage tmp   = questions[i];
            questions[i] = questions[j];
            questions[j] = tmp;
        }
    }

    private void ShowCurrentQuestion()
    {
        if (questions == null || questions.Count == 0 || questionText == null) return;
        int idx = currentIndex % questions.Count;
        questionText.text = questions[idx].aussage;
    }

    // ================================================================== Klick-Handler

    private void OnWahrClicked()
    {
        if (!gameRunning) return;
        bool correct = questions[currentIndex % questions.Count].antwort == true;
        RegisterAnswer(correct);
    }

    private void OnFalschClicked()
    {
        if (!gameRunning) return;
        bool correct = questions[currentIndex % questions.Count].antwort == false;
        RegisterAnswer(correct);
    }

    private void RegisterAnswer(bool correct)
    {
        if (correct) localScore++;
        else         localScore = Mathf.Max(localScore - 1, -20); // Untergrenze -20
        currentIndex++;
        UpdateScoreDisplay();
        ShowCurrentQuestion();
    }

    // ================================================================== UI aufbauen

    private void HideLegacyElements()
    {
        if (minigamePanel == null) return;
        string[] toHide = { "Button", "TextCounter" };
        foreach (string n in toHide)
        {
            var t = minigamePanel.transform.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    private void EnsureGameUI()
    {
        if (uiBuilt && gameContainer != null) return;
        if (minigamePanel == null)
        {
            Debug.LogError("[Minispiel09] minigamePanel fehlt – bitte im Prefab zuweisen.");
            return;
        }

        TMP_FontAsset font = ResolveFont();

        // Stretch-Container über das gesamte Panel
        var cGo = new GameObject("WahrFalschContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        // Score-Anzeige (oben, Anker unten)
        scoreText = CreateText(
            "ScoreText", gameContainer, font,
            ScoreFontSize, TextAlignmentOptions.Center,
            new Vector2(0f, ScoreY), new Vector2(420f, ScoreHeight),
            "Punkte: 0", anchorBottom: true);

        // Frage-Text (mittig, Anker unten)
        questionText = CreateText(
            "QuestionText", gameContainer, font,
            QuestionFontSize, TextAlignmentOptions.Center,
            new Vector2(0f, QuestionY), new Vector2(720f, QuestionHeight),
            "...", anchorBottom: true);
        questionText.enableWordWrapping = true;

        // WAHR-Button (links unten)
        wahrButton = CreateAnswerButton(
            "WahrButton", gameContainer, font,
            new Vector2(-ButtonHalfGap, Row1Y),
            "✓  WAHR", WahrColor, WahrHighlight, WahrPressed,
            OnWahrClicked);

        // FALSCH-Button (rechts unten)
        falschButton = CreateAnswerButton(
            "FalschButton", gameContainer, font,
            new Vector2(ButtonHalfGap, Row1Y),
            "✗  FALSCH", FalschColor, FalschHighlight, FalschPressed,
            OnFalschClicked);

        uiBuilt = true;
    }

    // ================================================================== Hilfsmethoden

    private TMP_FontAsset ResolveFont()
    {
        // LiberationSans SDF direkt laden – einzige Font mit vollständigem Umlaut-Support
        var lib = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (lib != null) return lib;
        if (infoText      != null && infoText.font      != null) return infoText.font;
        if (countdownText != null && countdownText.font != null) return countdownText.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void StretchToParent(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        r.pivot     = new Vector2(0.5f, 0.5f);
    }

    private static TMP_Text CreateText(
        string name, Transform parent, TMP_FontAsset font,
        float fontSize, TextAlignmentOptions alignment,
        Vector2 pos, Vector2 size, string text, bool anchorBottom)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        if (anchorBottom)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot     = new Vector2(0.5f, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
        }
        rect.anchoredPosition = pos;
        rect.sizeDelta        = size;

        var tmp           = go.GetComponent<TextMeshProUGUI>();
        tmp.font          = font;
        tmp.fontSize      = fontSize;
        tmp.alignment     = alignment;
        tmp.color         = Color.white;
        tmp.text          = text;
        return tmp;
    }

    private Button CreateAnswerButton(
        string name, Transform parent, TMP_FontAsset font,
        Vector2 pos, string label, Color normal, Color highlight, Color pressed,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect              = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = new Vector2(ButtonWidth, ButtonHeight);

        var img   = go.GetComponent<Image>();
        img.color = normal;

        var btn   = go.GetComponent<Button>();
        var cb    = btn.colors;
        cb.normalColor      = normal;
        cb.highlightedColor = highlight;
        cb.pressedColor     = pressed;
        cb.selectedColor    = highlight;
        cb.disabledColor    = BtnDisabled;
        btn.colors          = cb;

        // Label
        var lbl              = CreateText("Label", go.transform, font,
            36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, label, anchorBottom: false);
        lbl.fontStyle        = FontStyles.Bold;
        StretchToParent(lbl.rectTransform);

        btn.onClick.AddListener(onClick);
        return btn;
    }

    // ------------------------------------------------------------------ Sichtbarkeit

    private void SetGameplayUIVisible(bool visible)
    {
        if (questionText  != null) questionText.gameObject.SetActive(visible);
        if (scoreText     != null) scoreText.gameObject.SetActive(visible);
        if (wahrButton    != null) wahrButton.gameObject.SetActive(visible);
        if (falschButton  != null) falschButton.gameObject.SetActive(visible);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (wahrButton   != null) wahrButton.interactable   = interactable;
        if (falschButton != null) falschButton.interactable = interactable;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Punkte: {localScore}";
    }
}
