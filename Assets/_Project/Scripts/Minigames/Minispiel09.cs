using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Minispiel 09 – Wahr oder Falsch: Fakten-Aussagen bewerten.
/// Seed-RPC-Pattern für deterministische Fragereihenfolge.
/// </summary>
public class Minispiel09 : MinigameBase
{
    // ── Layout-Konstanten (Spielbereich, bottom-anchor) ───────────────────
    private const float ButtonWidth    = 240f;
    private const float ButtonHeight   = 70f;
    private const float ButtonHalfGap  = 130f;
    private const float BottomOffset   = 60f;
    private const float QuestionHeight = 130f;  // reduziert (vorher 170), passt in 249px-Spielfeld

    private static float Row1Y     => BottomOffset;
    private static float QuestionY => Row1Y + ButtonHeight + 20f;   // 150

    // ── Farben ────────────────────────────────────────────────────────────
    private static readonly Color WahrColor      = new Color(0.13f, 0.70f, 0.22f);
    private static readonly Color FalschColor    = new Color(0.82f, 0.16f, 0.16f);
    private static readonly Color WahrHighlight  = new Color(0.20f, 0.85f, 0.30f);
    private static readonly Color WahrPressed    = new Color(0.08f, 0.50f, 0.15f);
    private static readonly Color FalschHigh     = new Color(0.95f, 0.25f, 0.25f);
    private static readonly Color FalschPres     = new Color(0.60f, 0.10f, 0.10f);
    private static readonly Color BtnDisabled    = new Color(0.30f, 0.30f, 0.30f, 0.50f);

    // ── Datenmodell ───────────────────────────────────────────────────────
    [Serializable] private class Frage { public string aussage; public bool antwort; }
    [Serializable] private class Fragendatenbank { public Frage[] fragen; }

    // ── State ─────────────────────────────────────────────────────────────
    private int         currentIndex;
    private int         randomSeed;
    private List<Frage> questions;

    // ── Runtime-UI ───────────────────────────────────────────────────────
    private bool          uiBuilt;
    private RectTransform gameContainer;
    private TMP_Text      questionText;
    private Button        wahrButton;
    private Button        falschButton;

    // ── Seed-RPC ──────────────────────────────────────────────────────────

    [PunRPC]
    public void RpcSetSeed(int seed) => randomSeed = seed;

    public override void TriggerMinigameStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int seed = UnityEngine.Random.Range(0, 99999);
            photonView.RPC(nameof(RpcSetSeed), RpcTarget.All, seed);
        }
        base.TriggerMinigameStart();
    }

    // ── MinigameBase ──────────────────────────────────────────────────────

    protected override void SetupGame()
    {
        countdownTime = 30f;
        currentIndex  = 0;

        if (textBeschreibung != null)
            textBeschreibung.text = "Stimmt die Aussage?\nTippe WAHR oder FALSCH!";

        LoadQuestions();
        ShuffleQuestions();
        HideLegacyElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetButtonsInteractable(false);
        ShowCurrentQuestion();
    }

    protected override void StartActualGame()
    {
        SetButtonsInteractable(true);
    }

    protected override void EndActualGame()
    {
        SetButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    // ── Fragen ────────────────────────────────────────────────────────────

    private void LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "minispiel09_fragen.json");
        try
        {
            var db = JsonUtility.FromJson<Fragendatenbank>(File.ReadAllText(path));
            questions = new List<Frage>(db.fragen);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Minispiel09] Fehler: {e.Message}");
            questions = new List<Frage>
            {
                new Frage { aussage = "Die Erde ist rund.",       antwort = true  },
                new Frage { aussage = "Wasser ist trocken.",      antwort = false }
            };
        }
    }

    private void ShuffleQuestions()
    {
        if (questions == null || questions.Count == 0) return;
        var rng = new System.Random(randomSeed);
        for (int i = questions.Count - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            (questions[i], questions[j]) = (questions[j], questions[i]);
        }
    }

    private void ShowCurrentQuestion()
    {
        if (questions == null || questions.Count == 0 || questionText == null) return;
        questionText.text = questions[currentIndex % questions.Count].aussage;
    }

    // ── Klick-Handler ─────────────────────────────────────────────────────

    private void OnWahrClicked()
    {
        if (!gameRunning) return;
        RegisterAnswer(questions[currentIndex % questions.Count].antwort == true);
    }

    private void OnFalschClicked()
    {
        if (!gameRunning) return;
        RegisterAnswer(questions[currentIndex % questions.Count].antwort == false);
    }

    private void RegisterAnswer(bool correct)
    {
        // Untergrenze -20 bleibt erhalten
        int delta = correct ? 1 : (localScore > -20 ? -1 : 0);
        AddScore(delta);
        currentIndex++;
        ShowCurrentQuestion();
    }

    // ── UI aufbauen ───────────────────────────────────────────────────────

    private void HideLegacyElements()
    {
        if (minigamePanel == null) return;
        foreach (string n in new[] { "Button", "TextCounter" })
        {
            var t = minigamePanel.transform.Find(n);
            if (t != null) t.gameObject.SetActive(false);
        }
    }

    private void EnsureGameUI()
    {
        if (uiBuilt && gameContainer != null) return;
        if (minigamePanel == null) { Debug.LogError("[Minispiel09] minigamePanel fehlt."); return; }

        TMP_FontAsset font = ResolveFont();

        var cGo = new GameObject("WahrFalschContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        // Frage-Text
        questionText = CreateText("QuestionText", gameContainer, font,
            32f, TextAlignmentOptions.Center,
            new Vector2(0f, QuestionY), new Vector2(720f, QuestionHeight),
            "…", anchorBottom: true);
        questionText.textWrappingMode  = TextWrappingModes.Normal;
        questionText.enableAutoSizing  = true;
        questionText.fontSizeMin        = 18f;
        questionText.fontSizeMax        = 32f;

        // WAHR-Button (links)
        wahrButton = CreateAnswerButton("WahrButton", gameContainer, font,
            new Vector2(-ButtonHalfGap, Row1Y),
            "✓  WAHR", WahrColor, WahrHighlight, WahrPressed, OnWahrClicked);

        // FALSCH-Button (rechts)
        falschButton = CreateAnswerButton("FalschButton", gameContainer, font,
            new Vector2(ButtonHalfGap, Row1Y),
            "✗  FALSCH", FalschColor, FalschHigh, FalschPres, OnFalschClicked);

        uiBuilt = true;
    }

    // ── Sichtbarkeit ─────────────────────────────────────────────────────

    private void SetGameplayUIVisible(bool v)
    {
        if (questionText != null) questionText.gameObject.SetActive(v);
        if (wahrButton   != null) wahrButton.gameObject.SetActive(v);
        if (falschButton != null) falschButton.gameObject.SetActive(v);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (wahrButton   != null) wahrButton.interactable   = interactable;
        if (falschButton != null) falschButton.interactable = interactable;
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────

    private TMP_FontAsset ResolveFont()
    {
        var lib = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (lib != null) return lib;
        if (textBeschreibung != null && textBeschreibung.font != null) return textBeschreibung.font;
        if (countdownText    != null && countdownText.font    != null) return countdownText.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void StretchToParent(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        r.pivot     = new Vector2(0.5f, 0.5f);
    }

    private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font,
        float fontSize, TextAlignmentOptions align, Vector2 pos, Vector2 size,
        string text, bool anchorBottom)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        if (anchorBottom) { rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f); }
        else              { rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); }
        rect.anchoredPosition = pos; rect.sizeDelta = size;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = font; tmp.fontSize = fontSize; tmp.alignment = align; tmp.color = Color.white; tmp.text = text;
        return tmp;
    }

    private Button CreateAnswerButton(string name, Transform parent, TMP_FontAsset font,
        Vector2 pos, string label, Color normal, Color highlight, Color pressed,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        var img = go.GetComponent<Image>(); img.color = normal;
        var btn = go.GetComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor = normal; cb.highlightedColor = highlight;
        cb.pressedColor = pressed; cb.selectedColor = highlight; cb.disabledColor = BtnDisabled;
        btn.colors = cb;
        var lbl = CreateText("Label", go.transform, font, 36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, label, anchorBottom: false);
        lbl.fontStyle = FontStyles.Bold;
        StretchToParent(lbl.rectTransform);
        btn.onClick.AddListener(onClick);
        return btn;
    }
}
