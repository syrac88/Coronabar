using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minispiel 08 – Stroop-Effekt: Tippe die FARBE des Textes, nicht das Wort!
/// Ein Farbwort erscheint in einer anderen Farbe. Der Spieler muss die Tintenfarbe
/// antippen, nicht die Wortbedeutung. (+1 richtig, -1 falsch)
/// </summary>
public class Minispiel08 : MinigameBase
{
    // ------------------------------------------------------------------ Layout
    private const int   ColorCount        = 5;
    private const int   AnswerButtonCount = 4;

    private const float WordFontSize      = 80f;
    private const float WordHeight        = 100f;
    private const float ScoreFontSize     = 28f;
    private const float ScoreHeight       = 40f;

    private const float ButtonWidth       = 215f;
    private const float ButtonHeight      = 62f;
    private const float ButtonHalfGap     = 120f;   // halber Abstand (Mitte-zu-Mitte / 2)
    private const float ButtonRowGap      = 14f;    // vertikaler Abstand zwischen den Reihen
    private const float BottomOffset      = 60f;    // 60px Pflichtabstand zum unteren Rand (Balken)

    // Berechnete Y-Positionen (Ankerpunkt = unten-mitte des Panels, pivot.y = 0)
    // Alle Werte = Abstand der Unterkante des Elements von der Panel-Unterkante
    // Row1=60, Row2=136, Word=218, Score=332 → Score-Oberkante bei 372px
    // Panel-Mitte liegt bei 267px von unten → Score-Oberkante bei +105 vom Zentrum
    // InfoText-Unterkante liegt bei +101 vom Zentrum → passt knapp ✓
    private static float Row1Y  => BottomOffset;                         // 60
    private static float Row2Y  => Row1Y + ButtonHeight + ButtonRowGap;  // 136
    private static float WordY  => Row2Y + ButtonHeight + 20f;           // 218
    private static float ScoreY => WordY + WordHeight  + 14f;            // 332

    // ------------------------------------------------------------------ Farben
    private static readonly string[] ColorNames =
    {
        "ROT", "BLAU", "GRÜN", "GELB", "LILA"
    };

    private static readonly Color[] ColorValues =
    {
        new Color(0.92f, 0.18f, 0.18f), // ROT
        new Color(0.22f, 0.45f, 1.00f), // BLAU
        new Color(0.15f, 0.78f, 0.18f), // GRÜN
        new Color(1.00f, 0.88f, 0.08f), // GELB
        new Color(0.65f, 0.20f, 0.90f), // LILA
    };

    // Button-Hintergrund: dunkles, semi-transparentes Panel
    private static readonly Color BtnBg          = new Color(0.12f, 0.12f, 0.18f, 0.88f);
    private static readonly Color BtnBgHighlight  = new Color(0.22f, 0.22f, 0.30f, 0.92f);
    private static readonly Color BtnBgPressed    = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    private static readonly Color BtnBgDisabled   = new Color(0.12f, 0.12f, 0.18f, 0.40f);

    // ------------------------------------------------------------------ State
    private int   localScore;
    private int   correctColorIndex;        // Tintenfarbe des Stroop-Wortes
    private int[] buttonColorIndices;       // welche Farb-Indizes auf den 4 Buttons liegen

    private bool uiBuilt;
    private RectTransform  gameContainer;
    private TMP_Text       wordText;        // Das große Stroop-Wort in der Mitte
    private TMP_Text       scoreText;
    private readonly Button[]   answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels  = new TMP_Text[AnswerButtonCount];

    // ================================================================== MinigameBase-Implementierung

    protected override void SetupGame()
    {
        countdownTime      = 30f;
        localScore         = 0;
        buttonColorIndices = new int[AnswerButtonCount];

        HideLegacyPrefabElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        UpdateScoreDisplay();
        GenerateNewRound();
    }

    protected override void StartActualGame()
    {
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(true);

        // Spielregel als Erinnerung, sobald das Spiel losgeht
        if (infoText != null)
            infoText.text = "Tippe die FARBE des Textes – nicht das Wort!";
    }

    protected override void EndActualGame()
    {
        SetAnswerButtonsInteractable(false);
        SetGameplayUIVisible(false);
    }

    protected override float GetLocalPlayerScore() => localScore;

    // ================================================================== Legacy verstecken

    private void HideLegacyPrefabElements()
    {
        if (minigamePanel == null) return;

        Transform btn     = minigamePanel.transform.Find("Button");
        Transform counter = minigamePanel.transform.Find("TextCounter");
        if (btn     != null) btn.gameObject.SetActive(false);
        if (counter != null) counter.gameObject.SetActive(false);
    }

    // ================================================================== UI aufbauen

    private void EnsureGameUI()
    {
        if (uiBuilt && gameContainer != null) return;
        if (minigamePanel == null)
        {
            Debug.LogError("[Minispiel08] minigamePanel fehlt – bitte im Prefab zuweisen.");
            return;
        }

        TMP_FontAsset font = ResolveFont();

        // Streck-Container (füllt das minigamePanel aus)
        var containerGo = new GameObject("StroopContainer", typeof(RectTransform));
        containerGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = containerGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        // ---- Score-Anzeige (über dem Wort, Anker = unten-mitte) ----------
        scoreText = CreateText(
            "ScoreText", gameContainer, font,
            ScoreFontSize, TextAlignmentOptions.Center,
            new Vector2(0f, ScoreY), new Vector2(420f, ScoreHeight),
            "Punkte: 0", anchorToBottom: true);

        // ---- Großes Stroop-Wort (Anker = unten-mitte) --------------------
        wordText = CreateText(
            "WordText", gameContainer, font,
            WordFontSize, TextAlignmentOptions.Center,
            new Vector2(0f, WordY), new Vector2(680f, WordHeight),
            "FARBE", anchorToBottom: true);
        wordText.fontStyle = FontStyles.Bold;

        // ---- 4 Antwort-Buttons (2 × 2 Grid, Anker = unten-mitte) --------
        // Positionen: anchoredPosition ist der Mittelpunkt des Buttons (pivot.x=0.5)
        // und die untere Kante des Buttons (pivot.y=0).
        Vector2[] positions =
        {
            new Vector2(-ButtonHalfGap, Row1Y),   // unten-links
            new Vector2( ButtonHalfGap, Row1Y),   // unten-rechts
            new Vector2(-ButtonHalfGap, Row2Y),   // oben-links
            new Vector2( ButtonHalfGap, Row2Y),   // oben-rechts
        };

        for (int i = 0; i < AnswerButtonCount; i++)
            answerButtons[i] = CreateAnswerButton(i, gameContainer, font, positions[i]);

        for (int i = 0; i < AnswerButtonCount; i++)
            answerLabels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();

        uiBuilt = true;
    }

    // ================================================================== Runde generieren

    private void GenerateNewRound()
    {
        // Wortbedeutung ≠ Tintenfarbe (erzwungen)
        int wordMeaningIndex = Random.Range(0, ColorCount);
        int inkIndex;
        do { inkIndex = Random.Range(0, ColorCount); }
        while (inkIndex == wordMeaningIndex);

        correctColorIndex = inkIndex;

        // Wort anzeigen: zeigt ColorNames[wordMeaningIndex], Farbe = ColorValues[inkIndex]
        if (wordText != null)
        {
            wordText.text  = ColorNames[wordMeaningIndex];
            wordText.color = ColorValues[inkIndex];
        }

        // 4 verschiedene Farb-Indizes wählen, inkIndex muss enthalten sein
        List<int> pool = ShuffledRange(ColorCount);

        // Sicherstellen, dass inkIndex in den ersten 4 liegt
        if (!pool.GetRange(0, AnswerButtonCount).Contains(inkIndex))
        {
            int swapPos = Random.Range(0, AnswerButtonCount);
            int existingIdx = pool.IndexOf(inkIndex);
            // IndexOf findet inkIndex in position >= 4; tausche mit swapPos
            (pool[swapPos], pool[existingIdx]) = (pool[existingIdx], pool[swapPos]);
        }

        // Die ersten 4 Einträge des Pool noch einmal durchmischen (zufällige Buttonanordnung)
        for (int i = AnswerButtonCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            buttonColorIndices[i] = pool[i];
            if (answerLabels[i] != null)
            {
                int ci = pool[i];
                answerLabels[i].text  = ColorNames[ci];
                answerLabels[i].color = ColorValues[ci]; // Text IN der jeweiligen Farbe
            }
        }
    }

    private static List<int> ShuffledRange(int count)
    {
        var list = new List<int>(count);
        for (int i = 0; i < count; i++) list.Add(i);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }

    // ================================================================== Klick-Handler

    private void OnAnswerButtonClicked(int buttonIndex)
    {
        if (!gameRunning) return;

        bool correct = buttonColorIndices[buttonIndex] == correctColorIndex;
        if (correct) localScore++;
        else         localScore--;

        UpdateScoreDisplay();
        GenerateNewRound();
    }

    // ================================================================== Hilfsmethoden

    private TMP_FontAsset ResolveFont()
    {
        // LiberationSans SDF direkt laden – einzige Font mit vollständigem Umlaut-Support (z.B. GRÜN)
        var lib = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (lib != null) return lib;
        if (infoText      != null && infoText.font      != null) return infoText.font;
        if (countdownText != null && countdownText.font != null) return countdownText.font;
        return TMP_Settings.defaultFontAsset;
    }

    private static void StretchToParent(RectTransform r)
    {
        r.anchorMin  = Vector2.zero;
        r.anchorMax  = Vector2.one;
        r.offsetMin  = Vector2.zero;
        r.offsetMax  = Vector2.zero;
        r.pivot      = new Vector2(0.5f, 0.5f);
    }

    private static TMP_Text CreateText(
        string name, Transform parent, TMP_FontAsset font,
        float fontSize, TextAlignmentOptions alignment,
        Vector2 anchoredPos, Vector2 size,
        string initialText, bool anchorToBottom)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        if (anchorToBottom)
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
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = size;

        var tmp        = go.GetComponent<TextMeshProUGUI>();
        tmp.font       = font;
        tmp.fontSize   = fontSize;
        tmp.alignment  = alignment;
        tmp.color      = Color.white;
        tmp.text       = initialText;
        return tmp;
    }

    private Button CreateAnswerButton(
        int index, Transform parent, TMP_FontAsset font, Vector2 anchoredPos)
    {
        var go = new GameObject($"ColorButton_{index}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect           = go.GetComponent<RectTransform>();
        rect.anchorMin     = new Vector2(0.5f, 0f);
        rect.anchorMax     = new Vector2(0.5f, 0f);
        rect.pivot         = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta     = new Vector2(ButtonWidth, ButtonHeight);

        var img   = go.GetComponent<Image>();
        img.color = BtnBg;

        var btn   = go.GetComponent<Button>();
        var cb    = btn.colors;
        cb.normalColor      = BtnBg;
        cb.highlightedColor = BtnBgHighlight;
        cb.pressedColor     = BtnBgPressed;
        cb.selectedColor    = BtnBgHighlight;
        cb.disabledColor    = BtnBgDisabled;
        btn.colors = cb;

        // Label: Text IN der Farbe der jeweiligen Farbe (wird in GenerateNewRound gesetzt)
        var label      = CreateText("Label", go.transform, font,
            36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, "?", anchorToBottom: false);
        StretchToParent(label.rectTransform);

        int captured = index;
        btn.onClick.AddListener(() => OnAnswerButtonClicked(captured));
        return btn;
    }

    // ------------------------------------------------------------------ Sichtbarkeit & Interaktion

    private void SetGameplayUIVisible(bool visible)
    {
        if (wordText  != null) wordText.gameObject.SetActive(visible);
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
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
