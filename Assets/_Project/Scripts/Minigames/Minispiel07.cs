using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Minispiel 07 – Allgemeinwissen-Quiz: numerische Antworten, JSON-Datenbank.
/// Seed-RPC-Pattern für deterministisch gleiche Fragereihenfolge auf allen Clients.
/// </summary>
public class Minispiel07 : MinigameBase
{
    // ── Layout-Konstanten (Spielbereich, bottom-anchor) ───────────────────
    private const int   AnswerButtonCount       = 3;
    private const float ButtonBottomOffset      = 60f;
    private const float ButtonWidth             = 175f;
    private const float ButtonHeight            = 58f;
    private const float ButtonHorizontalSpacing = 195f;
    private const float QuestionToButtonGap     = 10f;
    private const float QuestionHeight          = 110f;  // etwas höher für Zeilenumbruch

    private static float QuestionBottomY => ButtonBottomOffset + ButtonHeight + QuestionToButtonGap;

    private static readonly Color ButtonBrown          = new Color(0.55f, 0.35f, 0.18f, 1f);
    private static readonly Color ButtonBrownHighlight = new Color(0.65f, 0.43f, 0.24f, 1f);
    private static readonly Color ButtonBrownPressed   = new Color(0.40f, 0.25f, 0.12f, 1f);
    private static readonly Color ButtonBrownDisabled  = new Color(0.35f, 0.28f, 0.22f, 0.6f);

    // ── Datenmodell ───────────────────────────────────────────────────────
    [Serializable] private class Frage { public string frage; public int richtig; public int[] falsch; }
    [Serializable] private class Fragendatenbank { public Frage[] fragen; }

    // ── State ─────────────────────────────────────────────────────────────
    private int          currentCorrectAnswer;
    private int          randomSeed;
    private List<Frage>  questions;
    private int          currentIndex;

    // ── Runtime-UI ───────────────────────────────────────────────────────
    private bool          uiBuilt;
    private RectTransform gameContainer;
    private TMP_Text      questionText;
    private readonly Button[]   answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels  = new TMP_Text[AnswerButtonCount];

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
            textBeschreibung.text = "Welche Antwort ist richtig?\n+1 richtig  |  -1 falsch";

        LoadQuestions();
        ShuffleQuestions();
        HideLegacyElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        ShowCurrentQuestion();
    }

    protected override void StartActualGame()
    {
        SetAnswerButtonsInteractable(true);
    }

    protected override void EndActualGame()
    {
        SetAnswerButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    // ── Fragen ────────────────────────────────────────────────────────────

    private void LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "minispiel07_fragen.json");
        try
        {
            var db = JsonUtility.FromJson<Fragendatenbank>(File.ReadAllText(path));
            questions = new List<Frage>(db.fragen);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Minispiel07] Fehler: {e.Message}");
            questions = new List<Frage>
            {
                new Frage { frage = "Wie viele Beine hat eine Spinne?", richtig = 8, falsch = new[] { 6, 10 } }
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
        if (questions == null || questions.Count == 0) return;
        var q = questions[currentIndex % questions.Count];
        currentCorrectAnswer = q.richtig;
        if (questionText != null) questionText.text = q.frage;

        var answers = new List<int>(q.falsch) { q.richtig };
        for (int i = answers.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (answers[i], answers[j]) = (answers[j], answers[i]);
        }
        for (int i = 0; i < AnswerButtonCount; i++)
            if (answerLabels[i] != null) answerLabels[i].text = answers[i].ToString();
    }

    // ── Klick-Handler ─────────────────────────────────────────────────────

    private void OnAnswerButtonClicked(int idx)
    {
        if (!gameRunning || idx < 0 || idx >= answerLabels.Length) return;
        if (!int.TryParse(answerLabels[idx].text, out int selected)) return;
        AddScore(selected == currentCorrectAnswer ? 1 : -1);
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
        if (minigamePanel == null) { Debug.LogError("[Minispiel07] minigamePanel fehlt."); return; }

        TMP_FontAsset font = ResolveFont();

        var cGo = new GameObject("QuizContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        questionText = CreateText("QuestionText", gameContainer, font,
            36f, TextAlignmentOptions.Center,
            new Vector2(0f, QuestionBottomY), new Vector2(760f, QuestionHeight),
            "Frage laden…", anchorBottom: true);
        questionText.enableAutoSizing  = true;
        questionText.fontSizeMin       = 18f;
        questionText.fontSizeMax       = 36f;
        questionText.textWrappingMode  = TextWrappingModes.Normal;

        float[] xPos = { -ButtonHorizontalSpacing, 0f, ButtonHorizontalSpacing };
        for (int i = 0; i < AnswerButtonCount; i++)
        {
            answerButtons[i] = CreateAnswerButton(i, gameContainer, font,
                new Vector2(xPos[i], ButtonBottomOffset));
            answerLabels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();
        }

        uiBuilt = true;
    }

    // ── Sichtbarkeit ─────────────────────────────────────────────────────

    private void SetGameplayUIVisible(bool v)
    {
        if (questionText != null) questionText.gameObject.SetActive(v);
        foreach (var b in answerButtons) if (b != null) b.gameObject.SetActive(v);
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (var b in answerButtons) if (b != null) b.interactable = interactable;
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

    private Button CreateAnswerButton(int idx, Transform parent, TMP_FontAsset font, Vector2 pos)
    {
        var go = new GameObject($"AnswerButton_{idx}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        var img = go.GetComponent<Image>(); img.color = ButtonBrown;
        var btn = go.GetComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor = ButtonBrown; cb.highlightedColor = ButtonBrownHighlight;
        cb.pressedColor = ButtonBrownPressed; cb.selectedColor = ButtonBrownHighlight;
        cb.disabledColor = ButtonBrownDisabled;
        btn.colors = cb;
        var lbl = CreateText("Label", go.transform, font, 36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, "?", anchorBottom: false);
        lbl.fontStyle = FontStyles.Bold;
        StretchToParent(lbl.rectTransform);
        int captured = idx;
        btn.onClick.AddListener(() => OnAnswerButtonClicked(captured));
        return btn;
    }
}
