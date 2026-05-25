using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minispiel 08 – Stroop-Effekt: Tippe die FARBE des Textes, nicht das Wort!
/// Spielbereich (bottom-anchor): 2×2 Button-Grid + großes Farbwort.
/// Score-Anzeige im Prefab-Header (textScore).
/// </summary>
public class Minispiel08 : MinigameBase
{
    // ── Layout-Konstanten (Spielbereich, bottom-anchor) ───────────────────
    private const int   ColorCount        = 5;
    private const int   AnswerButtonCount = 4;

    private const float WordFontSize  = 80f;
    private const float WordHeight    = 90f;   // reduziert (vorher 100), damit ins 249px-Spielfeld passt

    private const float ButtonWidth   = 215f;
    private const float ButtonHeight  = 62f;
    private const float ButtonHalfGap = 120f;
    private const float ButtonRowGap  = 14f;
    private const float BottomOffset  = 60f;

    // Y-Positionen (Anker = Panel-Unterkante, pivot.y = 0)
    // Row1=60, Row2=136, Word=218 → Word-Oberkante bei 308px < 309px (Header-Grenze) ✓
    private static float Row1Y => BottomOffset;
    private static float Row2Y => Row1Y + ButtonHeight + ButtonRowGap;
    private static float WordY => Row2Y + ButtonHeight + 20f;

    // ── Farb-Definitionen ─────────────────────────────────────────────────
    private static readonly string[] ColorNames =
        { "ROT", "BLAU", "GRÜN", "GELB", "LILA" };

    private static readonly Color[] ColorValues =
    {
        new Color(0.92f, 0.18f, 0.18f),
        new Color(0.22f, 0.45f, 1.00f),
        new Color(0.15f, 0.78f, 0.18f),
        new Color(1.00f, 0.88f, 0.08f),
        new Color(0.65f, 0.20f, 0.90f),
    };

    private static readonly Color BtnBg         = new Color(0.12f, 0.12f, 0.18f, 0.88f);
    private static readonly Color BtnHighlight  = new Color(0.22f, 0.22f, 0.30f, 0.92f);
    private static readonly Color BtnPressed    = new Color(0.06f, 0.06f, 0.10f, 0.92f);
    private static readonly Color BtnDisabled   = new Color(0.12f, 0.12f, 0.18f, 0.40f);

    // ── State ─────────────────────────────────────────────────────────────
    private int   correctColorIndex;
    private int[] buttonColorIndices;

    // ── Runtime-UI ───────────────────────────────────────────────────────
    private bool          uiBuilt;
    private RectTransform gameContainer;
    private TMP_Text      wordText;
    private readonly Button[]   answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels  = new TMP_Text[AnswerButtonCount];

    // ── MinigameBase ──────────────────────────────────────────────────────

    protected override void SetupGame()
    {
        countdownTime      = 30f;
        buttonColorIndices = new int[AnswerButtonCount];

        if (textBeschreibung != null)
            textBeschreibung.text = "Tippe die FARBE des Textes – nicht das Wort!";

        HideLegacyPrefabElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        GenerateNewRound();
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

    // ── Runde generieren ─────────────────────────────────────────────────

    private void GenerateNewRound()
    {
        int wordIdx = Random.Range(0, ColorCount);
        int inkIdx;
        do { inkIdx = Random.Range(0, ColorCount); } while (inkIdx == wordIdx);
        correctColorIndex = inkIdx;

        if (wordText != null)
        {
            wordText.text  = ColorNames[wordIdx];
            wordText.color = ColorValues[inkIdx];
        }

        var pool = ShuffledRange(ColorCount);
        if (!pool.GetRange(0, AnswerButtonCount).Contains(inkIdx))
        {
            int swapPos = Random.Range(0, AnswerButtonCount);
            int existingIdx = pool.IndexOf(inkIdx);
            (pool[swapPos], pool[existingIdx]) = (pool[existingIdx], pool[swapPos]);
        }
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
                answerLabels[i].text  = ColorNames[pool[i]];
                answerLabels[i].color = ColorValues[pool[i]];
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

    // ── Klick-Handler ─────────────────────────────────────────────────────

    private void OnAnswerButtonClicked(int idx)
    {
        if (!gameRunning) return;
        AddScore(buttonColorIndices[idx] == correctColorIndex ? 1 : -1);
        GenerateNewRound();
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
        if (minigamePanel == null) { Debug.LogError("[Minispiel08] minigamePanel fehlt."); return; }

        TMP_FontAsset font = ResolveFont();

        var cGo = new GameObject("StroopContainer", typeof(RectTransform));
        cGo.transform.SetParent(minigamePanel.transform, false);
        gameContainer = cGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        // Großes Stroop-Wort
        wordText = CreateText("WordText", gameContainer, font,
            WordFontSize, TextAlignmentOptions.Center,
            new Vector2(0f, WordY), new Vector2(680f, WordHeight),
            "FARBE", anchorBottom: true);
        wordText.fontStyle = FontStyles.Bold;

        // 2×2 Button-Grid
        Vector2[] positions =
        {
            new Vector2(-ButtonHalfGap, Row1Y), new Vector2(ButtonHalfGap, Row1Y),
            new Vector2(-ButtonHalfGap, Row2Y), new Vector2(ButtonHalfGap, Row2Y),
        };
        for (int i = 0; i < AnswerButtonCount; i++)
            answerButtons[i] = CreateAnswerButton(i, gameContainer, font, positions[i]);
        for (int i = 0; i < AnswerButtonCount; i++)
            answerLabels[i] = answerButtons[i].GetComponentInChildren<TMP_Text>();

        uiBuilt = true;
    }

    // ── Sichtbarkeit ─────────────────────────────────────────────────────

    private void SetGameplayUIVisible(bool v)
    {
        if (wordText != null) wordText.gameObject.SetActive(v);
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
        var go = new GameObject($"ColorButton_{idx}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f); rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = pos; rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
        var img = go.GetComponent<Image>(); img.color = BtnBg;
        var btn = go.GetComponent<Button>();
        var cb  = btn.colors;
        cb.normalColor = BtnBg; cb.highlightedColor = BtnHighlight;
        cb.pressedColor = BtnPressed; cb.selectedColor = BtnHighlight; cb.disabledColor = BtnDisabled;
        btn.colors = cb;
        var lbl = CreateText("Label", go.transform, font, 36f, TextAlignmentOptions.Center,
            Vector2.zero, rect.sizeDelta, "?", anchorBottom: false);
        StretchToParent(lbl.rectTransform);
        int captured = idx;
        btn.onClick.AddListener(() => OnAnswerButtonClicked(captured));
        return btn;
    }
}
