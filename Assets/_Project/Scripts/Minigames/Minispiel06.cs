using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minispiel 06 – Mathe-Duell (Erweitert): +/- mit zweistelligen Zahlen (10–99),
/// × mit einstelligen (1–9), ÷ mit sauberer Division (Ergebnis einstellig, kein Rest).
/// Spiel-UI wird zur Laufzeit erzeugt, um Inspector-Zuweisungen zu minimieren.
/// </summary>
public class Minispiel06 : MinigameBase
{
    private const int AnswerButtonCount = 3;
    private const float ButtonBottomOffset = 60f;  // 60px Pflichtabstand zum unteren Rand (Balken)
    private const float ButtonWidth = 175f;
    private const float ButtonHeight = 58f;
    private const float ButtonHorizontalSpacing = 195f;
    private const float QuestionToButtonGap = 10f;
    private const float ScoreToQuestionGap = 24f;
    private const float QuestionHeight = 72f;
    private const float ScoreHeight = 44f;

    private static float QuestionBottomY => ButtonBottomOffset + ButtonHeight + QuestionToButtonGap;
    private static float ScoreBottomY => QuestionBottomY + QuestionHeight + ScoreToQuestionGap;

    private static readonly Color ButtonBrown = new Color(0.55f, 0.35f, 0.18f, 1f);
    private static readonly Color ButtonBrownHighlight = new Color(0.65f, 0.43f, 0.24f, 1f);
    private static readonly Color ButtonBrownPressed = new Color(0.40f, 0.25f, 0.12f, 1f);

    // +, -, × (einstellig), ÷ (sauber, einstelliges Ergebnis)
    private static readonly char[] Operators = { '+', '-', 'x', '/' };

    private int localScore;
    private int currentCorrectAnswer;
    private bool uiBuilt;

    private RectTransform gameContainer;
    private TMP_Text questionText;
    private TMP_Text scoreText;
    private readonly Button[] answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels = new TMP_Text[AnswerButtonCount];

    protected override void SetupGame()
    {
        countdownTime = 30f;
        localScore = 0;
        HideLegacyPrefabElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        UpdateScoreDisplay();
        GenerateNewQuestion();
    }

    protected override void StartActualGame()
    {
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(true);
    }

    protected override void EndActualGame()
    {
        SetAnswerButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    protected override float GetLocalPlayerScore()
    {
        return localScore;
    }

    private void HideLegacyPrefabElements()
    {
        if (minigamePanel == null) return;

        Transform button = minigamePanel.transform.Find("Button");
        if (button != null) button.gameObject.SetActive(false);

        Transform counter = minigamePanel.transform.Find("TextCounter");
        if (counter != null) counter.gameObject.SetActive(false);
    }

    private void EnsureGameUI()
    {
        if (uiBuilt && gameContainer != null) return;
        if (minigamePanel == null)
        {
            Debug.LogError("Minispiel06: minigamePanel fehlt – bitte im Prefab zuweisen.");
            return;
        }

        TMP_FontAsset font = ResolveFont();

        GameObject containerGo = new GameObject(
            "MatheDuellContainer",
            typeof(RectTransform));
        containerGo.transform.SetParent(minigamePanel.transform, false);

        gameContainer = containerGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        questionText = CreateText(
            "QuestionText",
            gameContainer,
            font,
            52,
            TextAlignmentOptions.Center,
            new Vector2(0f, QuestionBottomY),
            new Vector2(700f, QuestionHeight),
            "10 + 20 = ?",
            anchorToBottom: true);

        scoreText = CreateText(
            "ScoreText",
            gameContainer,
            font,
            32,
            TextAlignmentOptions.Center,
            new Vector2(0f, ScoreBottomY),
            new Vector2(400f, ScoreHeight),
            "Punkte: 0",
            anchorToBottom: true);

        float[] buttonXPositions =
        {
            -ButtonHorizontalSpacing,
            0f,
            ButtonHorizontalSpacing
        };

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            answerButtons[i] = CreateAnswerButton(
                i,
                gameContainer,
                font,
                new Vector2(buttonXPositions[i], ButtonBottomOffset),
                OnAnswerButtonClicked);
            answerLabels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();
        }

        uiBuilt = true;
    }

    private TMP_FontAsset ResolveFont()
    {
        if (infoText != null && infoText.font != null) return infoText.font;
        if (countdownText != null && countdownText.font != null) return countdownText.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static TMP_Text CreateText(
        string name,
        Transform parent,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 anchoredPosition,
        Vector2 size,
        string initialText,
        bool anchorToBottom = false)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        if (anchorToBottom)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.text = initialText;

        return text;
    }

    private Button CreateAnswerButton(
        int index,
        Transform parent,
        TMP_FontAsset font,
        Vector2 anchoredPosition,
        UnityEngine.Events.UnityAction<int> onClick)
    {
        GameObject buttonGo = new GameObject(
            $"AnswerButton_{index}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);

        Image image = buttonGo.GetComponent<Image>();
        image.color = ButtonBrown;

        Button button = buttonGo.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonBrown;
        colors.highlightedColor = ButtonBrownHighlight;
        colors.pressedColor = ButtonBrownPressed;
        colors.selectedColor = ButtonBrownHighlight;
        colors.disabledColor = new Color(0.35f, 0.28f, 0.22f, 0.6f);
        button.colors = colors;

        TMP_Text label = CreateText(
            "Label",
            buttonGo.transform,
            font,
            36,
            TextAlignmentOptions.Center,
            Vector2.zero,
            rect.sizeDelta,
            "?");

        RectTransform labelRect = label.rectTransform;
        StretchToParent(labelRect);

        int capturedIndex = index;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(capturedIndex));

        return button;
    }

    private void OnAnswerButtonClicked(int buttonIndex)
    {
        if (!gameRunning || buttonIndex < 0 || buttonIndex >= answerLabels.Length) return;
        if (!int.TryParse(answerLabels[buttonIndex].text, out int selectedAnswer)) return;

        if (selectedAnswer == currentCorrectAnswer)
            localScore++;
        else
            localScore--;

        UpdateScoreDisplay();
        GenerateNewQuestion();
    }

    /// <summary>
    /// Generiert eine neue Matheaufgabe:
    /// +/- mit zweistelligen Zahlen (10–99), × mit einstelligen (1–9),
    /// / als saubere Division (Divisor 2–9, Quotient 2–9).
    /// </summary>
    private void GenerateNewQuestion()
    {
        char op = Operators[Random.Range(0, Operators.Length)];
        int a, b;

        if (op == '+' || op == '-')
        {
            // Zweistellige Operanden
            a = Random.Range(10, 100);
            b = Random.Range(10, 100);
            if (op == '-' && b > a) (a, b) = (b, a);
        }
        else if (op == 'x')
        {
            // Einstellige Operanden wie Minispiel05
            a = Random.Range(1, 10);
            b = Random.Range(1, 10);
        }
        else // '/'
        {
            // Saubere Division: Divisor (2–9) × Quotient (2–9) = Dividend
            int divisor = Random.Range(2, 10);
            int quotient = Random.Range(2, 10);
            a = divisor * quotient; // Dividend (max 81)
            b = divisor;
        }

        currentCorrectAnswer = op switch
        {
            '+' => a + b,
            '-' => a - b,
            'x' => a * b,
            '/' => a / b,
            _ => a + b
        };

        if (questionText != null)
            questionText.text = $"{a} {op} {b}";

        List<int> answers = BuildShuffledAnswers(currentCorrectAnswer);
        for (int i = 0; i < AnswerButtonCount; i++)
        {
            if (answerLabels[i] != null)
                answerLabels[i].text = answers[i].ToString();
        }
    }

    /// <summary>
    /// Erstellt eine gemischte Liste mit der richtigen und zwei falschen Antworten.
    /// Falschen Antworten werden per Offset generiert; negative Werte werden vermieden.
    /// </summary>
    private static List<int> BuildShuffledAnswers(int correctAnswer)
    {
        HashSet<int> uniqueAnswers = new HashSet<int> { correctAnswer };

        while (uniqueAnswers.Count < AnswerButtonCount)
        {
            int offset = Random.Range(1, 12);
            int candidate = Random.value > 0.5f
                ? correctAnswer + offset
                : correctAnswer - offset;

            if (candidate == correctAnswer)
                candidate += offset;

            // Negative oder Null-Antworten vermeiden
            if (candidate <= 0)
                candidate = correctAnswer + offset;

            uniqueAnswers.Add(candidate);
        }

        List<int> answers = new List<int>(uniqueAnswers);
        for (int i = 0; i < answers.Count; i++)
        {
            int swapIndex = Random.Range(i, answers.Count);
            (answers[i], answers[swapIndex]) = (answers[swapIndex], answers[i]);
        }

        return answers;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = $"Punkte: {localScore}";
    }

    private void SetAnswerButtonsInteractable(bool interactable)
    {
        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (questionText != null)
            questionText.gameObject.SetActive(visible);

        if (scoreText != null)
            scoreText.gameObject.SetActive(visible);

        foreach (Button button in answerButtons)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }
}
