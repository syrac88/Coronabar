using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json; // Assuming Newtonsoft.Json is available in your Unity project
using Photon.Pun; // Added for PhotonNetwork
using Photon.Realtime; // Added for Player, RoomInfo, FriendInfo, RegionHandler, EventData, IInRoomCallbacks

/// <summary>
/// Minispiel 07 – Allgemeinwissen-Quiz: Fragen mit numerischen Antworten unter Zeitdruck (+1 richtig, -1 falsch).
/// Fragen werden aus einer JSON-Datei geladen.
/// </summary>
public class Minispiel07 : MinigameBase, IInRoomCallbacks
{
    private const int AnswerButtonCount = 3;
    private const float ButtonBottomOffset = 55f;
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

    private int localScore;
    private int currentCorrectAnswer;
    private bool uiBuilt;

    private RectTransform gameContainer;
    private TMP_Text questionText;
    private TMP_Text scoreText;
    private readonly Button[] answerButtons = new Button[AnswerButtonCount];
    private readonly TMP_Text[] answerLabels = new TMP_Text[AnswerButtonCount];

    private List<QuestionData> allQuestions;
    private int currentQuestionIndex;

    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public int correctAnswer;
        public List<int> falseAnswers;
    }

    [System.Serializable]
    public class QuestionList
    {
        public List<QuestionData> questions;
    }

    protected override void SetupGame()
    {
        localScore = 0;
        HideLegacyPrefabElements();
        EnsureGameUI();
        SetGameplayUIVisible(true);
        SetAnswerButtonsInteractable(false);
        UpdateScoreDisplay();
        LoadQuestions();
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
            Debug.LogError("Minispiel07: minigamePanel fehlt – bitte im Prefab zuweisen.");
            return;
        }

        TMP_FontAsset font = ResolveFont();

        GameObject containerGo = new GameObject(
            "AllgemeinwissenContainer",
            typeof(RectTransform));
        containerGo.transform.SetParent(minigamePanel.transform, false);

        gameContainer = containerGo.GetComponent<RectTransform>();
        StretchToParent(gameContainer);

        questionText = CreateText(
            "QuestionText",
            gameContainer,
            font,
            36,  // Default font size
            TextAlignmentOptions.Center,
            new Vector2(0f, QuestionBottomY),
            new Vector2(850f, QuestionHeight + 40),  // Increased width and height
            "Frage laden...",
            anchorToBottom: true);
        questionText.enableAutoSizing = true;  // Enable dynamic sizing
        questionText.fontSizeMin = 12;  // Smaller minimum
        questionText.fontSizeMax = 36;  // Adjusted maximum
        questionText.enableWordWrapping = true;  // Allow wrapping if needed
        questionText.lineSpacing = -10f;  // Tighter line spacing

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
                36, // Added missing fontSize argument
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

    private TMP_Text CreateText(
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
        float fontSize,
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
            fontSize,
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

    private void LoadQuestions()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "questions_de.json");

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            QuestionList questionList = JsonConvert.DeserializeObject<QuestionList>(json);
            allQuestions = questionList.questions;
            Debug.Log($"Loaded {allQuestions.Count} questions from {filePath}");
        }
        else
        {
            Debug.LogError($"questions_de.json not found at {filePath}");
            allQuestions = new List<QuestionData>();
        }
    }

    private void GenerateNewQuestion()
    {
        if (allQuestions == null || allQuestions.Count == 0)
        {
            questionText.text = "Keine Fragen geladen!";
            SetAnswerButtonsInteractable(false);
            return;
        }

        // Synchronize question index via Photon Custom Properties if MasterClient
        if (PhotonNetwork.IsMasterClient)
        {
            currentQuestionIndex = Random.Range(0, allQuestions.Count);
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["CurrentQuestionIndex"] = currentQuestionIndex;
            PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
        }
        else
        {
            // Non-MasterClients wait for the MasterClient to set the index
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("CurrentQuestionIndex", out object indexObj))
            {
                currentQuestionIndex = (int)indexObj;
            }
            else
            {
                // Fallback if property not yet set (e.g., client joined mid-game)
                currentQuestionIndex = Random.Range(0, allQuestions.Count); // Use random for now, will sync later
            }
        }

        QuestionData questionData = allQuestions[currentQuestionIndex];
        currentCorrectAnswer = questionData.correctAnswer;

        if (questionText != null)
            questionText.text = questionData.question;

        List<int> answers = new List<int>(questionData.falseAnswers);
        answers.Add(questionData.correctAnswer);
        ShuffleList(answers);

        for (int i = 0; i < AnswerButtonCount; i++)
        {
            if (answerLabels[i] != null)
                answerLabels[i].text = answers[i].ToString();
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
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

    // Implement IInRoomCallbacks methods
    public void OnPlayerEnteredRoom(Player newPlayer) { }
    public void OnPlayerLeftRoom(Player otherPlayer) { }
    public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("CurrentQuestionIndex"))
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                currentQuestionIndex = (int)propertiesThatChanged["CurrentQuestionIndex"];
                // Regenerate question for non-master clients to ensure sync
                GenerateNewQuestion(); 
            }
        }
    }
    public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }
    public void OnMasterClientSwitched(Player newMasterClient) { }

    protected void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    protected void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }
}
