using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Minispiel04 : MinigameBase
{
    [Header("Spielspezifische UI")]
    public Button clickButton;
    public TMP_Text TextCounter;

    [Header("Einstellungen")]
    [SerializeField] private float fixedX_A = -350f;
    [SerializeField] private float fixedX_B = 350f;
    [SerializeField] private float fixedY_A = -30f;
    [SerializeField] private float fixedY_B = -195f;

    private int localClicks = 0;

    protected override void SetupGame()
    {
        localClicks = 0;
        clickButton.gameObject.SetActive(false);

        if (TextCounter != null)
        {
            TextCounter.gameObject.SetActive(false);
            TextCounter.text = "0";
        }

        clickButton.onClick.RemoveAllListeners();
        clickButton.onClick.AddListener(OnClickButtonPressed);
    }

    protected override void StartActualGame()
    {
        clickButton.gameObject.SetActive(true);
        if (TextCounter != null) TextCounter.gameObject.SetActive(true);
    }

    protected override void EndActualGame()
    {
        clickButton.gameObject.SetActive(false);
        if (TextCounter != null) TextCounter.gameObject.SetActive(false);
    }

    protected override float GetLocalPlayerScore()
    {
        return localClicks;
    }

    private void OnClickButtonPressed()
    {
        if (!gameRunning) return;

        localClicks++;
        if (TextCounter != null) TextCounter.text = localClicks.ToString();

        RectTransform buttonRect = clickButton.GetComponent<RectTransform>();
        float randomX = Random.Range(fixedX_A, fixedX_B);
        float randomY = Random.Range(fixedY_A, fixedY_B);

        buttonRect.anchoredPosition = new Vector2(randomX, randomY);
    }
}