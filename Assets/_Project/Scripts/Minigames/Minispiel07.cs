using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Minispiel 07 – Allgemeinwissen-Quiz: Fragen mit numerischen Antworten unter Zeitdruck.
/// 3 Antwort-Buttons (1 richtig, 2 falsch). +1 richtig, -1 falsch.
/// Fragen aus Assets/StreamingAssets/minispiel07_fragen.json.
/// Seed-RPC-Pattern: MasterClient sendet Seed → gleiche Fragereihenfolge auf allen Clients.
/// </summary>
public class Minispiel07 : MinigameBase
{
    // ------------------------------------------------------------------ Layout
    private const int   AnswerButtonCount        = 3;
    private const float ButtonBottomOffset       = 60f;  // 60px Pflichtabstand zum unteren Rand (Balken)
    private const float ButtonWidth              = 175f;
    private const float ButtonHeight             = 58f;
    private const float ButtonHorizontalSpacing  = 195f;
    private const float QuestionToButtonGap      = 10f;
    private const float ScoreToQuestionGap       = 24f;
    private const float QuestionHeight           = 110f;  // hoeher fuer Zeilenumbruch
    private const float ScoreHeight              = 44f;

    private static float QuestionBottomY => ButtonBottomOffset + ButtonHeight + QuestionToButtonGap;
    private static float ScoreBottomY    => QuestionBottomY + QuestionHeight + ScoreToQuestionGap;

    // ------------------------------------------------------------------ Farben
    private static readonly Color ButtonBrown          = new Color(0.55f, 0.35f, 0.18f, 1f);
    private static readonly Color ButtonBrownHighlight = new Color(0.65f, 0.43f, 0.24f, 1f);
    private static readonly Color ButtonBrownPressed   = new Color(0.40f, 0.25f, 0.12f, 1f);
    private static readonly Color ButtonBrownDisabled  = new Color(0.35f, 0.28f, 0.22f, 0.6f);

    // ------------------------------------------------------------------ Datenmodell
    [Serializable]
    private class Frage
    {
        public string frage;
        public int    richtig;
        public int[]  falsch;   // genau 2 Eintraege
    }

    [Serializable]
    private class Fragendatenbank
    {
        public Frage[] fragen;
    }

    // ------------------------------------------------------------------ State
    private int          localScore;
    private int          currentCorrectAnswer;
    private int          randomSeed;
    private List<Frage>  questions;
    private int          currentIndex;

    // ------------------------------------------------------------------ UI
    private bool          uiBuilt;
    private RectTransform gameContainer;
    private TMP_Text      questionText;
    private TMP_Text      scoreText;
    private readonly Button[]    answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[]  answerLabels  = new TMP_Text[AnswerButtonCount];

    // ================================================================== Seed-RPC

    [PunRPC]
    public void RpcSetSeed(int seed)
    {
        randomSeed = seed;
    }

    public override void TriggerMinigameStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int seed = UnityEngine.Random.Range(0, 99999);
            photonView.RPC(nameof(RpcSetSeed), RpcTarget.All, seed);
        }
        base.TriggerMinigameStart();
    }

    // ================================================================== MinigameBase

    protected override void SetupGame()
    {
        countdownTime        = 30f;
        localScore           = 0;
        currentIndex         = 0;

        LoadQuestions();
        ShuffleQuestions();
        HideLegacyElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        UpdateScoreDisplay();
        ShowCurrentQuestion();
    }

    protected override void StartActualGame()
    {
        SetAnswerButtonsInteractable(true);
        if (infoText != null)
            infoText.text = "Welche Antwort ist richtig?";
    }

    protected override void EndActualGame()
    {
        SetAnswerButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    protected override float GetLocalPlayerScore() => localScore;

    // ================================================================== Fragen

    private void LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "minispiel07_fragen.json");
        try
        {
            string json = File.ReadAllText(path);
            var db = JsonUtility.FromJson<Fragendatenbank>(json);
            questions = new List<Frage>(db.fragen);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Minispiel07] Fehler beim Laden der Fragen: {e.Message}");
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
            int j       = rng.Next(0, i + 1);
            Frage tmp   = questions[i];
            questions[i] = questions[j];
            questions[j] = tmp;
        }
    }

    private void ShowCurrentQuestion()
    {
        if (questions == null || questions.Count == 0) return;

        Frage q = questions[currentIndex % questions.Count];
        currentCorrectAnswer = q.richtig;

        if (questionText != null)
            questionText.text = q.frage;

        // Antworten mischen: 1 richtig + 2 falsch
        List<int> answers = new List<int>(q.falsch) { q.richtig };
        ShuffleList(answers);

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            if (answerLabels[i] != null)
                answerLabels[i].text = answers[i].ToString();
        }
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j    = UnityEngine.Random.Range(0, i + 1);
            T   tmp  = list[i];
            list[i]  = list[j];
            list[j]  = tmp;
        }
    }

    // ================================================================== Klick

    private void OnAnswerButtonClicked(int buttonIndex)
    {
        if (!gameRunning || buttonIndex < 0 || buttonIndex >= answerLabels.Length) return;
        if (!int.TryParse(answerLabels[buttonIndex].text, out int selected)) return;

        if (selected == currentCorrectAnswer) localScore++;
        else                                  localScore--;

        currentIndex++;
        UpdateScoreDisplay();
        ShowCurrentQuestion();
    }

    // ================================================================== UI aufbauen

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
        if (minigamePanel == null)
        {
            Debug.LogError("[Minispiel07] minigamePanel fehlt – bitte im Prefab zuweisen.");
            return;
        }

        TMP_FontAsset font = ResolveFont();

        var cGo = new GameObject("QuizContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        // Frage-Text (mit Auto-Sizing fuer lange Fragen)
        questionText = CreateText(
            "QuestionText", gameContainer, font,
            36f, TextAlignmentOptions.Center,
            new Vector2(0f, QuestionBottomY),
            new Vector2(760f, QuestionHeight),
            "Frage laden...", anchorBottom: true);
        questionText.enableAutoSizing  = true;
        questionText.fontSizeMin       = 18f;
        questionText.fontSizeMax       = 36f;
        questionText.enableWordWrapping = true;

        // Score
        scoreText = CreateText(
            "ScoreText", gameContainer, font,
            32f, TextAlignmentOptions.Center,
            new Vector2(0f, ScoreBottomY),
            new Vector2(400f, ScoreHeight),
            "Punkte: 0", anchorBottom: true);

        // 3 Antwort-Buttons
        float[] xPositions = { -ButtonHorizontalSpacing, 0f, ButtonHorizontalSpacing };
        for (int i = 0; i < AnswerButtonCount; i++)
        {
            answerButtons[i] = CreateAnswerButton(
                i, gameContainer, font,
                new Vector2(xPositions[i], ButtonBottomOffset));
            answerLabels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();
        }

        uiBuilt = true;
    }

    // ================================================================== Hilfsmethoden

    private TMP_FontAsset ResolveFont()
    {
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

        var tmp        = go.GetComponent<TextMeshProUGUI>();
        tmp.font       = font;
        tmp.fontSize   = fontSize;
        tmp.alignment  = alignment;
        tmp.color      = Color.white;
        tmp.text       = text;
        return tmp;
    }

    private Button CreateAnswerButton(int index, Transform parent, TMP_FontAsset font, Vector2 pos)
    {
        var go = new GameObject($"AnswerButton_{index}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect              = go.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos;
        rect.sizeDelta        = new Vector2(ButtonWidth, ButtonHeight);

        var img   = go.GetComponent<Image>();
        img.color = ButtonBrown;

        var btn   = go.GetComponent<Button>();
        var cb    = btn.colors;
        cb.normalColor      = ButtonBrown;
        cb.highlightedColor = ButtonBrownHighlight;
        cb.pressedColor     = ButtonBrownPressed;
        cb.selectedColor    = ButtonBrownHighlight;
        cb.disabledColor    = ButtonBrownDisabled;
        btn.colors          = cb;

        var lbl = CreateText("Label", go.transform, font,
            36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, "?", anchorBottom: false);
        lbl.fontStyle = FontStyles.Bold;
        StretchToParent(lbl.rectTransform);

        int captured = index;
        btn.onClick.AddListener(() => OnAnswerButtonClicked(captured));
        return btn;
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (questionText != null) questionText.gameObject.SetActive(visible);
        if (scoreText    != null) scoreText.gameObject.SetActive(visible);
        foreach (var b in answerButtons)
            if (b != null) b.gameObject.SetActive(visible);
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (var b in answerButtons)
            if (b != null) b.interactable = interactable;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Punkte: {localScore}";
    }
}
