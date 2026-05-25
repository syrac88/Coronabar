using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minispiel 06 – Mathe-Duell (Erweitert): +/- zweistellig, × einstellig, ÷ sauber.
/// </summary>
public class Minispiel06 : MinigameBase
{
    // ── Layout-Konstanten ─────────────────────────────────────────────────
    private const int   AnswerButtonCount       = 3;
    private const float ButtonBottomOffset      = 60f;
    private const float ButtonWidth             = 175f;
    private const float ButtonHeight            = 58f;
    private const float ButtonHorizontalSpacing = 195f;
    private const float QuestionToButtonGap     = 10f;
    private const float QuestionHeight          = 72f;

    private static float QuestionBottomY => ButtonBottomOffset + ButtonHeight + QuestionToButtonGap;

    private static readonly Color ButtonBrown          = new Color(0.55f, 0.35f, 0.18f, 1f);
    private static readonly Color ButtonBrownHighlight = new Color(0.65f, 0.43f, 0.24f, 1f);
    private static readonly Color ButtonBrownPressed   = new Color(0.40f, 0.25f, 0.12f, 1f);
    private static readonly char[] Operators = { '+', '-', 'x', '/' };

    // ── State ─────────────────────────────────────────────────────────────
    private int  currentCorrectAnswer;
    private bool uiBuilt;

    // ── Runtime-UI ───────────────────────────────────────────────────────
    private RectTransform  gameContainer;
    private TMP_Text       questionText;
    private readonly Button[]   answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels  = new TMP_Text[AnswerButtonCount];

    // ── MinigameBase ──────────────────────────────────────────────────────

    protected override void SetupGame()
    {
        countdownTime = 30f;

        if (textBeschreibung != null)
            textBeschreibung.text = "Löse die Rechenaufgabe!\n+1 richtig  |  -1 falsch";

        HideLegacyPrefabElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        GenerateNewQuestion();
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

    // ── Frage generieren ─────────────────────────────────────────────────

    private void GenerateNewQuestion()
    {
        char op = Operators[Random.Range(0, Operators.Length)];
        int a, b;

        if (op == '+' || op == '-')
        {
            a = Random.Range(10, 100); b = Random.Range(10, 100);
            if (op == '-' && b > a) (a, b) = (b, a);
        }
        else if (op == 'x')
        {
            a = Random.Range(1, 10); b = Random.Range(1, 10);
        }
        else // '/'
        {
            int div = Random.Range(2, 10); int quot = Random.Range(2, 10);
            a = div * quot; b = div;
        }

        currentCorrectAnswer = op switch
        {
            '+' => a + b, '-' => a - b, 'x' => a * b, '/' => a / b, _ => a + b
        };

        if (questionText != null) questionText.text = $"{a} {op} {b}";

        var answers = BuildShuffledAnswers(currentCorrectAnswer);
        for (int i = 0; i < AnswerButtonCount; i++)
            if (answerLabels[i] != null) answerLabels[i].text = answers[i].ToString();
    }

    private static List<int> BuildShuffledAnswers(int correct)
    {
        var unique = new HashSet<int> { correct };
        while (unique.Count < AnswerButtonCount)
        {
            int offset    = Random.Range(1, 12);
            int candidate = Random.value > 0.5f ? correct + offset : correct - offset;
            if (candidate == correct) candidate += offset;
            if (candidate <= 0)      candidate  = correct + offset;
            unique.Add(candidate);
        }
        var list = new List<int>(unique);
        for (int i = 0; i < list.Count; i++) { int j = Random.Range(i, list.Count); (list[i], list[j]) = (list[j], list[i]); }
        return list;
    }

    // ── Klick-Handler ─────────────────────────────────────────────────────

    private void OnAnswerButtonClicked(int idx)
    {
        if (!gameRunning || idx < 0 || idx >= answerLabels.Length) return;
        if (!int.TryParse(answerLabels[idx].text, out int selected)) return;
        AddScore(selected == currentCorrectAnswer ? 1 : -1);
        GenerateNewQuestion();
    }

    // ── UI aufbauen ───────────────────────────────────────────────────────

    private void HideLegacyPrefabElements()
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
        if (minigamePanel == null) { Debug.LogError("[Minispiel06] minigamePanel fehlt."); return; }

        TMP_FontAsset font = ResolveFont();

        var cGo = new GameObject("MatheDuellContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        questionText = CreateText("QuestionText", gameContainer, font,
            52f, TextAlignmentOptions.Center,
            new Vector2(0f, QuestionBottomY), new Vector2(700f, QuestionHeight),
            "10 + 20 = ?", anchorBottom: true);

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
        cb.disabledColor = new Color(0.35f, 0.28f, 0.22f, 0.6f);
        btn.colors = cb;
        var lbl = CreateText("Label", go.transform, font, 36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, "?", anchorBottom: false);
        StretchToParent(lbl.rectTransform);
        int captured = idx;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnAnswerButtonClicked(captured));
        return btn;
    }
}
